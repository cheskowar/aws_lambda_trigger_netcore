using System;
using System.ComponentModel.DataAnnotations;

namespace ProcesaArchivo.Models {
    public class ControlDescarga {
        public long idArchivoIns {get; set;}
        public int idDes {get; set;}
        public DateTime fechaInicio {get; set;}
        public DateTime? fechaFin {get; set;}
        public string estado {get; set;}
        public decimal? tamanoMB {get; set;}
        public string tipoDesc {get; set;}
        public long? idUsuario {get; set;}
    }
}