using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ProcesaArchivo.Models;
using RestSharp;
using Newtonsoft.Json;
using System;
namespace ProcesaArchivo.Services
{
    public class NotificacionesService : INotificacionesService
    {
        private readonly ILogger _logger;
        private IDBService _dbService;
        private EnvVariables _envVariables;

        public NotificacionesService(ILogger<NotificacionesService> logger, IDBService db_service, EnvVariables envVar)
        {
            _logger = logger;
            _dbService = db_service;
            _envVariables = envVar;
        }

        public void NotificaError(string message)
        {
            _logger.LogError(message);
            string json = "";
            IRestResponse response = null;
            Models.Notificaciones model = new Models.Notificaciones();

            model.id_tipo_notificacion = Convert.ToInt64(_envVariables.tipoNotificacion).ToString();
            model.descripcion = message;
            model.id_carga = _envVariables.ControlCarga.idCarga.ToString();
            model.id_descarga = _envVariables.ControlDescarga.idDes;
            model.parametros = new List<parametros>(); 
            model.parametros.Add(new parametros() { idAI =  _envVariables.ArchivoInstancia.id,  servicio = "Carga - ProcesaArchivo"  });
            json = JsonConvert.SerializeObject(model);
            _logger.LogInformation("json: {0}", json);
            try
            {
                var client = new RestClient(_envVariables.UrlNotificaciones);
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("undefined", json, ParameterType.RequestBody);
                response = client.Execute(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            finally
            {

                if (Convert.ToInt16(response.StatusCode) != 200)
                {
                    _logger.LogError("Respuesta servicio Content:{0}", response.Content);
                    _logger.LogError("Respuesta servicio Exception:{0}", response.ErrorException);
                    _logger.LogError("Respuesta servicio ErrorMessage:{0}", response.ErrorMessage);
                }
            }
        }
    }

    /// INTERFAZ
    public interface INotificacionesService
    {
        void NotificaError(string message);
    }
}