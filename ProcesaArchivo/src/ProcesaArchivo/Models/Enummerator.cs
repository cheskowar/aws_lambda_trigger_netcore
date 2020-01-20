namespace ProcesaArchivo.Models
{
    public class Enummerator
    {
        public enum TipoNotificacion
        {
            //Validar Id´s de dev vs QA
            SinDefinir = 0,
            NoSePudoSubirArchivoS3 = 79,
            NoSePudoSubirMetadatosArchivoS3 = 80,
            ArchivoEspecificoNoExiste = 81,
            NumeroArchivosInternoNoconcide = 82,
            IncidenciasCarga = 63
        }
    }
}