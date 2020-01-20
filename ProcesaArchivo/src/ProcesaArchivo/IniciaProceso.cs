using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProcesaArchivo.Models;
using ProcesaArchivo.Services;


class IniciaProceso : IIniciaProceso
{
    private readonly ILogger _logger;
    private IAWSService _awsService;
    private IDBService _dbService;
    private IFileService _fileService;
    private EnvVariables _envVariables;

    public IniciaProceso(ILogger<IniciaProceso> logger, IAWSService aws_service, IDBService db_service, IFileService file_service, EnvVariables env_variables)
    {
        _logger = logger;
        _awsService = aws_service;
        _dbService = db_service;
        _fileService = file_service;
        _envVariables = env_variables;
    }

    public async Task Init()
    {
        try
        {
            // Se descarga el archivo.
            _logger.LogInformation("Inicia procesamiento del archivo {0}.", _envVariables.nombreArchivo);
            // Descarga el archivo desde el S3.
            await _awsService.DownloadFileToS3();
            // Cargar datos del archivo desde la BD.
            await _dbService.GetDataArchivo();

            var path = Path.GetFullPath(_envVariables.pathTempLocal);
            var extension_archivo = Path.GetExtension(path).ToUpper();
            _logger.LogInformation("El archivo tiene extensión: {0}", extension_archivo);
            // Descomprime el archivo y procesa los archivos internos.
            if (extension_archivo.Equals(".ZIP"))
            {
                _fileService.DescomprimeArchivo();
                foreach (var item in _envVariables.ArchivosInternos)
                {
                    _logger.LogInformation("Path Temporal: {0}", item.PathTemporal);
                    await ProcesaPorExtension(item.PathTemporal, item.numeroInterno);
                }
            }
            else // Procesa el archivo.
                await ProcesaPorExtension(path);

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Algo salio mal :(");
        }
    }
    DateTime fechaCDMX()
    {
        DateTime fechaUTC = DateTime.UtcNow;
        TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
        //TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
        DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(fechaUTC, cstZone);
        return cstTime;
    }


    public async Task ProcesaPorExtension(string path, long idInterno = 0)
    {
        var extension_archivo = Path.GetExtension(path).ToUpper();
        //guarda en ControlCarga
        DateTime fechaInicioCarga = fechaCDMX();
        ControlCarga data = new ControlCarga()
        {
            fechaInicio = fechaInicioCarga,
            fechaFin = null,
            estado = "Inicia Carga",
            numeroRegistros = null,
            numeroRegistrosBuenos = null,
            numeroRegistrosMalos = null,
            idDescarga = _envVariables.ControlDescarga.idDes,
            idArchivoInterno = idInterno,
            partesProcesadas = null,
            idArchivoInstancia = _envVariables.ArchivoInstancia.id,
            idCarga = _envVariables.ControlCarga == null ? 1 : _envVariables.ControlCarga.idCarga + 1
        };
        _logger.LogInformation("El archivo intenta guardar con los siguientes datos");
        _logger.LogInformation("fechaInicio: {0}", data.fechaInicio);
        _logger.LogInformation("fechaFin: {0}", data.fechaFin);
        await _dbService.SetControlCarga(data);
        _envVariables.ControlCarga = data;
        _envVariables.estado = "Archivo Segmentado";
        // Se procesa archivo.
        switch (extension_archivo)
        {
            case ".XLS":
                _fileService.convertirXls(path, idInterno);
                break;
            case ".XLSX":
                _fileService.procesarXlsx(path, idInterno);
                break;
            case ".CSV":
                _fileService.convertirXls(path, idInterno);
                break;
            default:
                 await _fileService.DivideCargaArchivo(path, idInterno);
                break;
        }

        data.fechaInicio = fechaInicioCarga;
        data.estado = _envVariables.estado;
        //  numeroRegistros = null,//_envVariables.totalRegistros,
        //  partesProcesadas = null,//_envVariables.totalSubArchivos,
        await _dbService.UpdateControlCarga(data);

    }
}

/// Interfaz de AWSService
public interface IIniciaProceso
{
    Task Init();
}