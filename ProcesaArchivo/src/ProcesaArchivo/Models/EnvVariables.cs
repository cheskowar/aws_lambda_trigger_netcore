using System.Collections.Generic;

namespace ProcesaArchivo.Models
{
    public class EnvVariables
    {
        public string EnvironmentTag { get; set; }      // Ambiente [dev, qa, prod].
        public string ArnLambdaCarga { get; set; }      // ARN de lambda encargada de procesar las partes del archivo.
        public string UrlNotificaciones { get; set; }   // URL de API de notificaciones.
        public string BucketS3 { get; set; }       // Nombre bucket del que se descarga el archivo.
        // public string PrefijoCarpetaS3_To { get; set; }     // Prefijo/Carpeta del S3 donde se depositaran las partes del archivo.
        public string PrefijoCarpetaS3 { get; set; }   // Prefijo/Carpeta del S3 donde se buscara el archivo.
        public int NumeroRegistrosPorParte { get; set; }  // Numero de registros que tendrá cada parte de las que se dividira el archivo.
        public string pathTempLocal { get; set; }       // Path temporal donde se descarga el archivo para partirlo.
        // Parametros al invocar la app.
        public string idVersionFileS3 { get; set; }     // IdVersion del archivo en el S3.
        public string nombreArchivo { get; set; }       // Nombre/Key del archivo a buscar en el S3.
        // Datos para el procesamiento del archivo.
        public Archivo Archivo { get; set; }              // Modelo de TB_Archivo.
        public ArchivoInstancia ArchivoInstancia { get; set; }    // Modelo de TB_ArchivosInstancias.
        public List<ArchivoInterno> ArchivosInternos { get; set; }    // Listado de modelos de TB_ArchivoInterno.
        public ControlDescarga ControlDescarga { get; set; }      // Modelo de TB_ControlDescarga.
        public ControlCarga ControlCarga { get; set; }      // Modelo de TB_ControlCarga.
        public Enummerator.TipoNotificacion tipoNotificacion { get; set; } //Establec el tipo de noticiacaion

        public int totalRegistros { get; set; }  // Indica el total de registros por archivo
        public int totalRegistrosArchivo { get; set; }  // Indica el total de sub archivos por archivo
        public double totalSubArchivos { get; set; }  // Indica el total de sub archivos por archivo
        public string estado { get; set; }  // Indica el estado de la descarga
    }
}