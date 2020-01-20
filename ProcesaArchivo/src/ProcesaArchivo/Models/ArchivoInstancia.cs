using System;

namespace ProcesaArchivo.Models {
    public class ArchivoInstancia {
        public long id {get; set;}
        public long idArchivo {get; set;}
        public DateTime fechaOperacion {get; set;}
        public DateTime rangoFechaInicio {get; set;}
        public DateTime rangoFechaFin {get; set;}
        public string status {get; set;}
    }
}