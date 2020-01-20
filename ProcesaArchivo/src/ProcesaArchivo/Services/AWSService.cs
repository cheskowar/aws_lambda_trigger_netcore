using Amazon.S3;
using Amazon.S3.Model;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Threading;
using System;
using Amazon.S3.Transfer;
using ProcesaArchivo.Models;
using System.IO.Compression;
using System.Linq;
using Amazon;
using Amazon.Runtime;

namespace ProcesaArchivo.Services
{
    public class AWSService : IAWSService
    {
        private readonly ILogger _logger;
        private IAmazonS3 _S3Client;
        private EnvVariables _envVariables;
        private INotificacionesService _notificacionesService;

        public AWSService(ILogger<AWSService> logger, 
                        // IAmazonS3 s3_client,
                        INotificacionesService notificaciones_service, EnvVariables envVar)
        {
            _logger = logger;
            _S3Client = new AmazonS3Client(
                Environment.GetEnvironmentVariable("ACCESS_KEY"),
                Environment.GetEnvironmentVariable("SECRET_KEY"),
                RegionEndpoint.USEast1
            );
            _envVariables = envVar;
            _notificacionesService = notificaciones_service;
        }

        /// <summary>
        /// Descarga el archivo desde el bucket definido y lo almacena en la carpeta
        /// temporal del contendor.
        /// </summary>
        /// <returns>El path, donde se almacenó de forma local el archivo.</returns>
        public async Task DownloadFileToS3()
        {
            // string responseBody = "";
            try
            {
                _logger.LogInformation("Inicia descarga desde S3.");
                var request = new GetObjectRequest
                {
                    BucketName = _envVariables.BucketS3,
                    Key = _envVariables.PrefijoCarpetaS3 + "/" + _envVariables.nombreArchivo,
                    VersionId = _envVariables.idVersionFileS3
                    // EtagToMatch = _envVariables.eTagFile
                };

                using (GetObjectResponse response = await _S3Client.GetObjectAsync(request))
                // using (Stream responseStream = response.ResponseStream)
                // using (StreamReader reader = new StreamReader(responseStream))
                {
                    _envVariables.ControlDescarga = new ControlDescarga
                    {
                        idArchivoIns = long.Parse(response.Metadata["x-amz-meta-idarchivoinstancia"]),
                        idDes = int.Parse(response.Metadata["x-amz-meta-iddescarga"])
                    };
                    // _envVariables.ArchivosInternos = new List<ArchivoInterno>(){
                    //     new ArchivoInterno {
                    //         numeroInterno = int.Parse(response.Metadata["x-amz-meta-idinterno"])
                    //     }
                    // };
                    string contentType = response.Headers["Content-Type"];

                    // _logger.LogDebug("idArchivoInstancia: {0}", idarchivoinstancia);
                    // _logger.LogDebug("idDescarga: {0}", iddescarga);
                    // _logger.LogDebug("idInterno: {0}", idinterno);
                    _logger.LogDebug("Content type: {0}", contentType);
                    // var pathTemp = "tmp/"; 
                    var pathTemp = Path.GetTempPath(); //(string.IsNullOrEmpty(_envVariables.pathTempLocal)) ?
                                    //Path.GetTempPath() : _envVariables.pathTempLocal;
                    string dest = Path.Combine(pathTemp, _envVariables.nombreArchivo);
                    _logger.LogDebug("PATH temporal: " + dest);
                    if (File.Exists(dest))
                    {
                        File.Delete(dest);
                    }
                    await response.WriteResponseStreamToFileAsync(dest, true, CancellationToken.None);
                    _logger.LogInformation("El archivo {0} se descargó correctamente en {1}.",
                                            _envVariables.nombreArchivo, dest);
                    _envVariables.pathTempLocal = dest;
                }
            }
            catch (AmazonS3Exception e)
            {
                if (e.ErrorCode != null &&
                    (e.ErrorCode.Equals("InvalidAccessKeyId") ||
                    e.ErrorCode.Equals("InvalidSecurity")))
                {
                    RegistraError(e.InnerException, "Error con las credenciales de AWS, asegurate que sean correctas.");
                }
                else if (e.ErrorCode.Equals("InvalidBucketName"))
                    RegistraError(e.InnerException, "El bucket: " + _envVariables.BucketS3 + " no existe.");
                else if (e.ErrorCode.Equals("NoSuchVersion"))
                {
                    _envVariables.tipoNotificacion = Enummerator.TipoNotificacion.ArchivoEspecificoNoExiste;
                    RegistraError(e.InnerException, "El archivo: " +
                                                    _envVariables.nombreArchivo + " no existe en en S3.");

                }
                else
                {
                    RegistraError(e.InnerException, "Ocurrio un error con mensaje: " +
                                                    e.Message + " cuando se obtenía un objeto en S3.");
                }
            }
            catch (Exception e)
            {
                var message = "Ocurrio un error con mensaje: " + e.Message;
                RegistraError(e, message);
            }
        }

        public async Task UploadPartFileToS3(string file_path, string file_name, long idInterno = 0)
        {
            try
            {
                var fileTransferUtility = new TransferUtility(_S3Client);
                var keyFile = _envVariables.Archivo.CarpetaS3 + "/" + file_name;
                var fileTransferUtilityRequest = new TransferUtilityUploadRequest()
                {
                    BucketName = _envVariables.BucketS3,
                    FilePath = file_path,
                    StorageClass = S3StorageClass.Standard,
                    // PartSize = 1, // 6 MB.
                    Key = keyFile,
                    CannedACL = S3CannedACL.AuthenticatedRead
                };
                string[] filename_dividido = file_name.Split('.');
                if (filename_dividido[1].ToLower() != "xlsx")
                {
                    using (var readerLines = new StreamReader(file_path))
                    {
                        var file = readerLines.ReadToEnd();
                        var lines = file.Split(new char[] { '\n' });
                        _envVariables.totalRegistrosArchivo = lines.Count() - 1;
                    }
                }

                _logger.LogDebug("La parte {0} se subio a la carpeta {1}.", file_name, _envVariables.Archivo.CarpetaS3);
                fileTransferUtilityRequest.Metadata.Add("idArchivo", _envVariables.Archivo.id.ToString());
                fileTransferUtilityRequest.Metadata.Add("idArchivoInstancia", _envVariables.ArchivoInstancia.id.ToString());
                fileTransferUtilityRequest.Metadata.Add("idDescarga", _envVariables.ControlDescarga.idDes.ToString());
                fileTransferUtilityRequest.Metadata.Add("idArchivoInterno", idInterno.ToString());
                fileTransferUtilityRequest.Metadata.Add("totalPartes", _envVariables.totalSubArchivos.ToString());
                fileTransferUtilityRequest.Metadata.Add("totalRegistros",(_envVariables.totalRegistros - _envVariables.Archivo.Encabezado).ToString());
                fileTransferUtilityRequest.Metadata.Add("noRegistros", _envVariables.totalRegistrosArchivo.ToString());
                fileTransferUtilityRequest.Metadata.Add("idControlCarga", _envVariables.ControlCarga.id.ToString());
                if (filename_dividido[1].ToLower() != "xlsx")
                    await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
                else
                    fileTransferUtility.Upload(fileTransferUtilityRequest);
                _logger.LogDebug("La parte {0} se subio a la carpeta {1}, con sus metadatos. idArchivo:{2}, idArchivoInstancia:{3},idDescarga:{4}, idArchivoInstancia:{5}", file_name, _envVariables.Archivo.CarpetaS3, _envVariables.Archivo.id.ToString(), _envVariables.ArchivoInstancia.id.ToString().ToString(), _envVariables.ControlDescarga.idDes.ToString(), idInterno.ToString());

            }
            catch (AmazonS3Exception e)
            {
                RegistraError(e.InnerException, "Error al subir la parte [" + file_name + "] al S3.");
                _envVariables.tipoNotificacion = Enummerator.TipoNotificacion.NoSePudoSubirArchivoS3;
                _notificacionesService.NotificaError(e.InnerException.ToString());
            }
            catch (Exception ex)
            {
                RegistraError(ex.InnerException, "Error al subir metadatos de la parte [" + file_name + "] al S3.");
                _envVariables.tipoNotificacion = Enummerator.TipoNotificacion.NoSePudoSubirMetadatosArchivoS3;
                _notificacionesService.NotificaError(ex.InnerException.ToString());
            }
        }

        private void RegistraError(Exception e, string message)
        {
            _logger.LogError(e, message);

            _notificacionesService.NotificaError(message);
        }


    }


    /// Interfaz de AWSService
    public interface IAWSService
    {
        Task DownloadFileToS3();
        Task UploadPartFileToS3(string file_path, string file_name, long idInterno = 0);
    }
}