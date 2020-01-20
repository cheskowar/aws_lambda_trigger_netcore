namespace ProcesaArchivo.Models {
    public class Archivo {
        public long? id {get; set;}
        public string NombreArchivo {get; set;}
        public string Alias {get; set;}
        public string ExtensionArchivo {get; set;}
        public int Encabezado {get; set;}
        public string Separador {get; set;}
        public string CarpetaS3 {get; set;}        
        public long idLayout {get; set;}
    }
}