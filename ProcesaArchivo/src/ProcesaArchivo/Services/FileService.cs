using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProcesaArchivo.Models;
using OfficeOpenXml;
using ExcelDataReader;
using System.Data;
using Syncfusion.XlsIO;

namespace ProcesaArchivo.Services
{
    class FileService : IFileService
    {
        private readonly ILogger _logger;
        private readonly IDBService _dbService;
        private EnvVariables _envVariables;
        private INotificacionesService _notificacionesService;
        private IAWSService _awsService;
        private string _archivoParteTemporal = Path.GetTempPath() + "newfile.txt";
        double tama = 2;
        public FileService(ILogger<FileService> logger, INotificacionesService notificaciones_service,
                            IDBService dB_service, IAWSService aws_service, EnvVariables envVar)
        {
            _logger = logger;
            _dbService = dB_service;
            _envVariables = envVar;
            _notificacionesService = notificaciones_service;
            _awsService = aws_service;
        }

        public void DescomprimeArchivo()
        {
            try
            {
                using (ZipArchive archivo = ZipFile.OpenRead(_envVariables.pathTempLocal))
                {
                    // Valida que el numero de archivos internos sea el mismo que en BD.
                    if (_envVariables.ArchivosInternos != null && archivo.Entries.Count() == _envVariables.ArchivosInternos.Count())
                    {
                        // Verifca que el nombre del archivo_interno este declarado en BD.
                        foreach (ZipArchiveEntry entry in archivo.Entries)
                        {
                            if (NombreArchivoInternoEsCorrecto(entry.Name).Result)
                            {
                                var destinationPath = Path.GetTempPath() + entry.Name;
                                if (File.Exists(destinationPath))
                                {
                                    File.Delete(destinationPath);
                                }
                                entry.ExtractToFile(destinationPath, true);
                                _envVariables.ArchivosInternos.Find(x => (x.PathTemporal ?? "") != "")
                                            .PathTemporal = destinationPath;
                                _logger.LogInformation("El archivo se descomprimío en el path: {0}", destinationPath);
                            }
                        }
                    }
                    else
                        _logger.LogError("El número de archivos internos no coincide." +
                                        " {0} Internos en ZIP vs {1} Internos definidos en BD.",
                                            archivo.Entries.Count(), (_envVariables.ArchivosInternos == null) ? 0 : _envVariables.ArchivosInternos.Count());
                }
            }
            catch (Exception e)
            {
                RegistraError(e, "Error al descomprimir el archivo.");
            }
        }

        private async Task<bool> NombreArchivoInternoEsCorrecto(string nombre_archivo_interno)
        {
            try
            {
                int posLlaveInicial = -1, posLlaveFinal = -1;
                var fecha = _envVariables.ArchivoInstancia.fechaOperacion;
                var formatosFechas = await _dbService.GetFormatosFechas();
                foreach (var archivoInterno in _envVariables.ArchivosInternos)
                {
                    var nombre_archivo = archivoInterno.NombreArchivo;
                    do
                    {
                        posLlaveInicial = nombre_archivo.IndexOf("{");
                        posLlaveFinal = nombre_archivo.IndexOf("}");
                        string subString = "", valor = "", resultado = "";
                        if (posLlaveInicial != -1 && posLlaveFinal != -1)
                        {
                            subString = nombre_archivo.Substring(posLlaveInicial + 1, posLlaveFinal - posLlaveInicial - 1);
                            if (!formatosFechas.Exists(x => x.origen.Equals(subString)))
                            {
                                Console.WriteLine("Nombre de Archivo sin formato correcto: " + subString);
                                return false;
                            }
                            valor = formatosFechas.Find(x => x.origen.Equals(subString)).destino;
                            switch (valor)
                            {
                                case "dano": resultado = diasTranscurridosDelAno(fecha).ToString(); break;
                                case "AA":
                                    string cadenaFecha = fecha.Year.ToString();
                                    resultado = cadenaFecha.Substring(cadenaFecha.Length - 2, 2); break;
                                //case "Mmm": resultado = primerCaracterMayus(fecha.ToString("MMMM")); break;

                                case "Ran(4)":
                                    var rnd = new Random(DateTime.Now.Millisecond);
                                    resultado = rnd.Next(5001, 9999).ToString();
                                    break;
                                default: resultado = fecha.ToString(valor); break;
                            }
                            nombre_archivo = nombre_archivo.Replace("{" + subString + "}", resultado);
                        }
                    } while (posLlaveInicial != -1);
                    if (nombre_archivo.ToLower().Equals(nombre_archivo_interno.ToLower(), StringComparison.OrdinalIgnoreCase))
                    {
                        _envVariables.ArchivosInternos.First(i => i.id == archivoInterno.id)
                                    .PathTemporal = nombre_archivo_interno;
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("El nombre de archivo {0} y el nombre del archivo interno no coinciden {1}: " + nombre_archivo, nombre_archivo_interno);
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                RegistraError(e, "Error al validar el nombre del archivo interno.");
                return false;
            }

        }

        public async Task DivideCargaArchivo(string path, long idInterno = 0)
        {
            string[] extensionArchivo = path.Split('.');
            int counterAllFiles = 0;
            string nombre_parte;
            int partesArchivo = 0;
            int maxLinesParte = _envVariables.NumeroRegistrosPorParte;
            _envVariables.totalRegistros = 0;
            _envVariables.totalSubArchivos = 0;
            _envVariables.totalRegistrosArchivo = 0;
            try
            {
                File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
                using (var readerLines = new StreamReader(path))
                {
                    var file = readerLines.ReadToEnd();
                    var lines = file.Split(new char[] { '\n' });
                    _envVariables.totalRegistros = lines.Count() - 1;
                    _envVariables.totalSubArchivos = 0;
                    if (_envVariables.totalRegistros > 0)
                    {
                        do
                        {
                            _envVariables.totalSubArchivos++;
                        }
                        while ((_envVariables.totalSubArchivos * maxLinesParte) < _envVariables.totalRegistros);
                    }

                }


                using (var reader = new StreamReader(path))
                {
                    string line;
                    int count = 0;
                    StreamWriter writer;
                    if (File.Exists(_archivoParteTemporal))
                    {
                        await File.WriteAllTextAsync(_archivoParteTemporal, "");
                        writer = File.AppendText(_archivoParteTemporal);
                    }
                    else
                        writer = File.CreateText(_archivoParteTemporal);

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        await writer.WriteLineAsync(line);
                        count++;
                        counterAllFiles++;

                        if (count >= maxLinesParte)
                        {
                            count = 0;
                            writer.Close();
                            partesArchivo++;
                            string[] nombre_dividido = _envVariables.nombreArchivo.Split('.');
                            nombre_parte = nombre_dividido[0] + "___" + partesArchivo + "." + extensionArchivo[extensionArchivo.Length - 1].ToLower();
                            await _awsService.UploadPartFileToS3(_archivoParteTemporal, nombre_parte, idInterno);
                            await File.WriteAllTextAsync(_archivoParteTemporal, "");
                            writer = File.AppendText(_archivoParteTemporal);
                        }
                    }
                    if (count > 0)
                    {
                        writer.Close();
                        partesArchivo++;
                        string[] nombre_dividido = _envVariables.nombreArchivo.Split('.');
                        nombre_parte = nombre_dividido[0] + "___" + partesArchivo + "." + extensionArchivo[extensionArchivo.Length - 1].ToLower();
                        await _awsService.UploadPartFileToS3(_archivoParteTemporal, nombre_parte);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al dividir el archivo.");
                _envVariables.estado = "Error";
            }
            finally
            {
                File.Delete(_archivoParteTemporal);
                File.Delete(path);
                _logger.LogInformation("El archivo tiene un total de {0} registros.", counterAllFiles);
                _logger.LogInformation("El archivo se dividio en {0} partes con {1} registros cada una.", partesArchivo, maxLinesParte);
            }
        }


        public void convertirXls(string rutaFile, long idInterno)
        {
            string xlsxFile = rutaFile.Split('.')[0] + ".xlsx";
            _logger.LogInformation("Si existe el archivo xlsx {0} lo borra.", xlsxFile);
            if (File.Exists(xlsxFile))
                File.Delete(xlsxFile);
            try
            {
                try
                {
                    _logger.LogInformation("Intenta abrir el archivo xls {0}.", rutaFile);
                    ExcelPackage package = new ExcelPackage();
                    try
                    {
                        DataTableCollection dts;
                        using (var stream = File.Open(rutaFile, FileMode.Open, FileAccess.Read))
                        {
                            _logger.LogInformation("Usa stream para el archivo xls {0}.", rutaFile);
                            using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                            {
                                DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
                                {
                                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                                });
                                dts = result.Tables;
                                _logger.LogInformation("Obtiene tablas el archivo xlsx {0}, {1} tablas.", xlsxFile, result.Tables.Count);
                            }
                        }
                        _logger.LogInformation("Abrió el archivo xls {0}.", rutaFile);
                        _logger.LogInformation("Intenta escribir el archivo xlsx {0}.", xlsxFile);
                        using (ExcelEngine excelEngine = new ExcelEngine())
                        {
                            IApplication application = excelEngine.Excel;
                            application.DefaultVersion = ExcelVersion.Excel2016;
                            IWorkbook workbook = application.Workbooks.Create(1);
                            IWorksheet worksheet = workbook.Worksheets[0];
                            DataTable dataTable = dts[0];
                            worksheet.ImportDataTable(dataTable, true, 1, 1);
                            worksheet.UsedRange.AutofitColumns();
                            using (FileStream file_stream = new FileStream(xlsxFile, FileMode.Create))
                            {
                                _logger.LogInformation("Usa stream para el archivo xlsx {0}.", xlsxFile);
                                workbook.SaveAs(file_stream);
                                _logger.LogInformation("Guardó  en stream el archivo xlsx {0}.", xlsxFile);
                            }
                        }
                        _logger.LogInformation("Escribió el archivo xlsx {0}.", xlsxFile);
                    }
                    catch (Exception e)
                    {
                        RegistraError(e, "No se pudo abrir el XLSX");
                        return;
                    }

                }
                catch (Exception e)
                {
                    RegistraError(e, "No se pudo abrir el XLSX");
                    return;
                }

                // using (ExcelPackage excel = new ExcelPackage())
                // {
                //     excel.Workbook.Worksheets.Add("Worksheet1");
                //     excel.Workbook.Worksheets.Add("Worksheet2");
                //     excel.Workbook.Worksheets.Add("Worksheet3");

                //     FileInfo excelFile = new FileInfo(rutaFile.Split('.')[0] + "xlsx");
                //     excel.SaveAs(excelFile);
                // }
            }
            catch (Exception ex)
            {

            }

            procesarXlsx(xlsxFile, idInterno);
        }

        double ConvertBytesToMegabytes(long bytes)
        {
            return (bytes / 1024f) / 1024f;
        }
        double obtenerTamano(string file)
        {
            FileInfo info = new FileInfo(file);
            //System.Console.WriteLine("Calculando Tamaño...");
            double size = ConvertBytesToMegabytes(info.Length);
            return size;
        }
        public void procesarXlsx(string ruta_loc_server, long idInterno)
        {
            _logger.LogInformation("Inicia proceso de archivo xlsx {0}.", ruta_loc_server);
            double size = obtenerTamano(ruta_loc_server);
            double sizemb = tama;
            string fileName = Path.GetFileName(ruta_loc_server);
            string[] filename_dividido = ruta_loc_server.Split('.');
            string[] nombre_dividido = fileName.Split('.');
            string[] nombreSimple = nombre_dividido[0].Split('\\');
            int rows, cols;
            ExcelPackage package = new ExcelPackage();
            try
            {
                package = new ExcelPackage(new FileInfo(ruta_loc_server));
            }
            catch (Exception e)
            {
                RegistraError(e, "No se pudo abrir el XLSX");
                return;
            }
            try
            {
                ExcelWorksheet workSheet = package.Workbook.Worksheets[0];
                _envVariables.totalRegistros = workSheet.Dimension.End.Row;
                _envVariables.totalRegistrosArchivo = workSheet.Dimension.End.Row;
                if (workSheet.Cells[_envVariables.totalRegistros, 1].Value == null)
                    rows = busquedaBinariaReng(1, _envVariables.totalRegistros, workSheet);
                cols = workSheet.Dimension.End.Column;
                string nombre = "";
                if (size > sizemb)
                {
                    _envVariables.totalSubArchivos = Math.Ceiling(size / sizemb);
                    int celdas = (int)Math.Ceiling(_envVariables.totalRegistros / _envVariables.totalSubArchivos);
                    int cuenta = 0;
                    int cu = 0;
                    int i = 0;
                    int j = i + 1;
                    while (i < _envVariables.totalSubArchivos)
                    {
                        _archivoParteTemporal = filename_dividido[0] + "___" + i + "." + filename_dividido[1];
                        if (File.Exists(_archivoParteTemporal))
                        {
                            File.Delete(_archivoParteTemporal);
                        }
                        var newFile = new FileInfo(_archivoParteTemporal);
                        ExcelPackage package2 = new ExcelPackage();
                        try
                        {
                            package2 = new ExcelPackage(newFile);
                        }
                        catch (Exception e)
                        {
                            RegistraError(e, "No se pudo abrir el XLSX");
                        }
                        ExcelWorksheet worksheet2 = package2.Workbook.Worksheets.Add("Sheet1");
                        worksheet2.Cells[1, 1, celdas, cols].Value = workSheet.Cells[1 + celdas * i, 1, celdas + (1 + (celdas * i)), cols].Value;
                        _envVariables.totalRegistrosArchivo = worksheet2.Dimension.End.Row;
                        i = i + 1;
                        try
                        {
                            package2.Save();
                        }
                        catch (Exception e)
                        {
                            RegistraError(e, "Error al guardar el archivo");
                        }
                        if (cuenta < _envVariables.totalRegistros)
                        {
                            cu = celdas;
                            cuenta = cuenta + celdas;
                        }
                        else
                            cu = _envVariables.totalRegistros - cuenta;
                        nombre = nombreSimple[0] + "___" + i + "." + filename_dividido[1];
                        _awsService.UploadPartFileToS3(_archivoParteTemporal, nombre, idInterno);
                        // subirArchivoS3(nombre, nombre_dividido[0] + "-_" + idDescarga + "_" + idInterno + "_" + j + "_" + cu + "." + nombre_dividido[1], carpeta);
                        // sppControlDescargaFin(idDescarga, j, fecha, (int)tam, idInterno);
                    }
                }
                else
                {
                    nombre = nombreSimple[0] + "___1." + filename_dividido[1];
                    _awsService.UploadPartFileToS3(ruta_loc_server, nombre, idInterno);
                    //     subirArchivoS3(ruta_loc_server, nombre_dividido[0] + "-_" + idDescarga + "_" + idInterno + "_0_" + rows + "." + nombre_dividido[1], carpeta);
                    //     sppControlDescargaFin(idDescarga, 1, fecha, 1, idInterno);
                }
            }
            catch (Exception ex)
            {
                RegistraError(ex, "Error al guardar el archivo");
            }

        }

        int busquedaBinariaReng(int min, int max, ExcelWorksheet workSheet)
        {
            double div = (max + min) / 2;
            int medio = (int)Math.Ceiling(div);
            if (workSheet.Cells[medio, 1].Value == null & workSheet.Cells[medio + 1, 1].Value == null & workSheet.Cells[medio + 3, 1].Value == null)
            {
                return busquedaBinariaReng(1, medio, workSheet);
            }
            else
            {
                if (workSheet.Cells[medio + 1, 1].Value == null & workSheet.Cells[medio + 2, 1].Value == null & workSheet.Cells[medio + 3, 1].Value == null)
                {
                    return medio;
                }
                else
                {
                    return busquedaBinariaReng(medio, max, workSheet);
                }
            }
        }



        private int diasTranscurridosDelAno(DateTime fecha)
        {
            TimeSpan elapsed = fecha.Subtract(DateTime.Parse(fecha.Year.ToString() + "/01/01"));
            return (int)elapsed.TotalDays + 1;
        }

        private void RegistraError(Exception e, string message)
        {
            _logger.LogError(e, message);

            _notificacionesService.NotificaError(message);
        }
    }

    public interface IFileService
    {
        void DescomprimeArchivo();
        Task DivideCargaArchivo(string path, long idInterno);
        void convertirXls(string ruta_loc_server, long idInterno);
        void procesarXlsx(string ruta_loc_server, long idInterno);
    }
}