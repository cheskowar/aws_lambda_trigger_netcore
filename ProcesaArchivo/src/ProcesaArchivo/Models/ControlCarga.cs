using System;
namespace ProcesaArchivo.Models
{
    public class ControlCarga
    {  
        public long? id { get; set; }
        public DateTime? fechaInicio { get; set; }
        public DateTime? fechaFin { get; set; }
        public string estado { get; set; }
        public int? numeroRegistros { get; set; }
        public int? numeroRegistrosBuenos { get; set; }
        public int? numeroRegistrosMalos { get; set; }
        public long? idDescarga { get; set; }
        public long? idArchivoInterno { get; set; }
        public int? partesProcesadas { get; set; }
        public long? idArchivoInstancia { get; set; }
        public long? idCarga { get; set; }
    }
}