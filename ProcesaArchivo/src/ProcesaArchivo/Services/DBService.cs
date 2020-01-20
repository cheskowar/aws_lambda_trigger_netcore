using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcesaArchivo.Models;

namespace ProcesaArchivo.Services
{
    class DBService : IDBService
    {
        private readonly ILogger _logger;
        private EnvVariables _envVariables;
        private ArchivoDBContext _archivoDbContext;
        private INotificacionesService _notificacionesService;

        public DBService(ILogger<DBService> logger, ArchivoDBContext dBContext,
                        // INotificacionesService notificaciones_service,
                        EnvVariables envVar)
        {
            _logger = logger;
            _envVariables = envVar;
            _archivoDbContext = dBContext;
            // _notificacionesService = notificaciones_service;
        }

        public async Task GetDataArchivo()
        {
            try
            {
                _envVariables.ArchivoInstancia = await _archivoDbContext.TB_ArchivosInstancias
                            .SingleOrDefaultAsync(x => x.id == _envVariables.ControlDescarga.idArchivoIns);
                _envVariables.Archivo = await _archivoDbContext.TB_Archivo
                                .SingleOrDefaultAsync(x => x.id == _envVariables.ArchivoInstancia.idArchivo);
                _envVariables.ControlDescarga = await _archivoDbContext.TB_ControlDescarga
                                .SingleOrDefaultAsync(x => x.idArchivoIns == _envVariables.ArchivoInstancia.id
                                            && x.idDes == _envVariables.ControlDescarga.idDes
                                            && x.estado=="Descargado");
                _envVariables.ArchivosInternos = await _archivoDbContext.TB_ArchivoInterno
                                .Where(x => x.id == _envVariables.Archivo.id).ToListAsync();
                _envVariables.ControlCarga = await _archivoDbContext.TB_ControlCarga.OrderByDescending(x=>x.fechaInicio)
                .FirstOrDefaultAsync(x => x.idArchivoInstancia == _envVariables.ArchivoInstancia.id
                && x.idDescarga == _envVariables.ControlDescarga.idDes);
                _logger.LogInformation("Los datos del archivo se cargaron desde la BD.");
            }
            catch (SqlException e)
            {
                var errorMessages = new StringBuilder();
                for (int i = 0; i < e.Errors.Count; i++)
                {
                    errorMessages.Append("Index #" + i + "\n" +
                        "Message: " + e.Errors[i].Message + "\n" +
                        "LineNumber: " + e.Errors[i].LineNumber + "\n" +
                        "Source: " + e.Errors[i].Source + "\n" +
                        "Procedure: " + e.Errors[i].Procedure + "\n");
                }
                RegistraError(e.InnerException, errorMessages.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Ocurrio un error al cargar los datos.");
                if (e.InnerException.Message.ToString().Contains("Object reference not set to an instance of an object.")
                && _envVariables.ControlCarga == null)
                {
                    _envVariables.ControlCarga = new ControlCarga();
                    _envVariables.ControlCarga.idCarga = 1;
                }
                else
                {
                    RegistraError(e, "Ocurrio un error al cargar los datos del archivo desde la BD.");
                }
            }

        }

        public async Task SetControlCarga(ControlCarga data)
        {
            try
            {
                await _archivoDbContext.AddAsync(data);
                await _archivoDbContext.SaveChangesAsync();
                _logger.LogInformation("Los datos de controlCarga se guardaron en la BD.");
            }
            catch (SqlException e)
            {
                var errorMessages = new StringBuilder();
                for (int i = 0; i < e.Errors.Count; i++)
                {
                    errorMessages.Append("Index #" + i + "\n" +
                        "Message: " + e.Errors[i].Message + "\n" +
                        "LineNumber: " + e.Errors[i].LineNumber + "\n" +
                        "Source: " + e.Errors[i].Source + "\n" +
                        "Procedure: " + e.Errors[i].Procedure + "\n");
                }
                RegistraError(e.InnerException, errorMessages.ToString());
            }
            catch (Exception e)
            {
                RegistraError(e, "Ocurrio un error al cargar los datos del archivo desde la BD.");
            }
        }

        public async Task UpdateControlCarga(ControlCarga data)
        {
            try
            {
                _archivoDbContext.Update(data);
                await _archivoDbContext.SaveChangesAsync();
                _logger.LogInformation("Los datos de controlCarga se actualizaron en la BD.");
            }
            catch (SqlException e)
            {
                var errorMessages = new StringBuilder();
                for (int i = 0; i < e.Errors.Count; i++)
                {
                    errorMessages.Append("Index #" + i + "\n" +
                        "Message: " + e.Errors[i].Message + "\n" +
                        "LineNumber: " + e.Errors[i].LineNumber + "\n" +
                        "Source: " + e.Errors[i].Source + "\n" +
                        "Procedure: " + e.Errors[i].Procedure + "\n");
                }
                RegistraError(e.InnerException, errorMessages.ToString());
            }
            catch (Exception e)
            {
                RegistraError(e, "Ocurrio un error al cargar los datos del archivo desde la BD.");
            }
        }

        public async Task<List<FormatosFechas>> GetFormatosFechas()
        {
            try
            {
                return await _archivoDbContext.CAT_EquivalenciaFechas.ToListAsync();
            }
            catch (Exception e)
            {
                RegistraError(e, "Ocurrio un problema al obtener las equivalencias de fechas.");
                return null;
            }
        }


        private void RegistraError(Exception e, string message)
        {
            _logger.LogError(e, message);

            _notificacionesService.NotificaError(message);
        }
    }

    public interface IDBService
    {
        Task GetDataArchivo();
        Task<List<FormatosFechas>> GetFormatosFechas();
        Task SetControlCarga(ControlCarga data);
        Task UpdateControlCarga(ControlCarga data);
    }
}