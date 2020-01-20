using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ProcesaArchivo.Models {
    public class ArchivoDBContext : DbContext {
        // private readonly IConfiguration _configuration;
        public ArchivoDBContext(ILogger<ArchivoDBContext> logger, 
                                // IConfiguration configuration,
                                DbContextOptions dbContextOptions):base(dbContextOptions)
        {
            // _configuration = configuration;
        }
        public DbSet<Archivo> TB_Archivo {get; set;}
        public DbSet<ArchivoInterno> TB_ArchivoInterno {get; set;}
        public DbSet<ControlDescarga> TB_ControlDescarga {get; set;}
        public DbSet<ArchivoInstancia> TB_ArchivosInstancias {get; set;}
       // public DbQuery<ControlCarga> TB_ControlCarga {get; set;}
        public DbSet<ControlCarga> TB_ControlCarga{get; set;}
        public DbSet<FormatosFechas> CAT_EquivalenciaFechas {get; set;}

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<ControlDescarga>().HasNoKey();

            base.OnModelCreating(builder);
        }
    }
}