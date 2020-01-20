using System.Collections.Generic;

namespace ProcesaArchivo.Models
{
   public class Notificaciones
    {
        public string id_tipo_notificacion { get; set; }
        public string descripcion { get; set; }
        public string id_carga { get; set; }
        public long? id_descarga { get; set; }
        public long? id_archivo { get; set; }
        public List<parametros> parametros { get; set; }
    }
    public class parametros
    {
        public long idAI { get; set; }
        public string servicio  { get; set; }
    }
}