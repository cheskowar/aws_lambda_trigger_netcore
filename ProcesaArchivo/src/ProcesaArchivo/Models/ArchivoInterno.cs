using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcesaArchivo.Models {
    public class ArchivoInterno {
        public long id {get; set;}
        public int numeroInterno {get; set;}  
        public long idLayout {get; set;}
        public string NombreArchivo {get; set;}
        public string ExtensionArchivo {get; set;}
        public int? Encabezado {get; set;}
        public string Separador {get; set;}
        [NotMapped]
        public string PathTemporal {get;set;}
        
    }
}