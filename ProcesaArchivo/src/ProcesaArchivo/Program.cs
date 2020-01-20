using ProcesaArchivo.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Amazon.S3;
using System.Threading.Tasks;
using System.IO;
using Microsoft.EntityFrameworkCore;
using ProcesaArchivo.Services;
using System;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Runtime.CredentialManagement;
using Amazon;

namespace ProcesaArchivo
{
    public class Program
    {
        const string _appsettings = "appsettings.json";
        static IConfiguration Configuration;
        static EnvVariables envVariables = new EnvVariables();
        static IIniciaProceso initial;

        private static async Task Main(string[] args)
        {
            // Inicializa la configuración de la app.
            Configuration = ConfigurationBuilder(Environment.GetEnvironmentVariable("ENVIRONMENT")).Build();
            // envVariables = Configuration.GetSection("environmentDM").Get<EnvVariables>();
            GetVariablesEntorno();
            // Configura la colección de servicios para la app.
            var services = ConfigureServices(Configuration);
            // Genera un proveedor para iniciar el servicio AWSService.
            var serviceProvider = services.BuildServiceProvider();
            // Inicia el Proceso.
            initial = serviceProvider.GetService<IIniciaProceso>();

            Func<S3Event, ILambdaContext, string> func = FunctionHandler;
            
            
            // Para debuguear como app de consola:
            //     - Descomentar la región DEBUG.
            //     - Comentar la región AWS_LAMBDA.

            // #region DEBUG
                // var context = new TestLambdaContext();
                // var s3_event = JsonConvert.DeserializeObject<S3Event>(await File.ReadAllTextAsync("event.json"));
                // var upperCase = Program.FunctionHandler(s3_event, context);
            //#endregion
            //FIXME Descomentar para que funcione como lambda.
            using (var handlerWrapper = HandlerWrapper.GetHandlerWrapper(func, new Amazon.Lambda.Serialization.Json.JsonSerializer()))
            using (var bootstrap = new LambdaBootstrap(handlerWrapper))
            {
                await bootstrap.RunAsync();
            }
        }

        private static void GetVariablesEntorno()
        {
            envVariables = new EnvVariables
            {
                NumeroRegistrosPorParte = Int32.Parse(Environment.GetEnvironmentVariable("NUMERO_REGISTROS_POR_PARTE")),
                UrlNotificaciones = Environment.GetEnvironmentVariable("URL_NOTIFICACIONES"),
                PrefijoCarpetaS3 = Environment.GetEnvironmentVariable("PREFIJO_CARPETA_S3"),
                BucketS3 = Environment.GetEnvironmentVariable("BUCKET_S3"),
                pathTempLocal = Environment.GetEnvironmentVariable("PathTempLocal")
            };
        }

        public static string FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            var s3Event = evnt.Records?[0].S3;
            if (s3Event == null)
            {
                Console.WriteLine("Error, el evento viene nulo.");
                return null;
            }
            var archivo = envVariables;
            envVariables.nombreArchivo = Path.GetFileName(s3Event.Object.Key);
            envVariables.idVersionFileS3 = s3Event.Object.VersionId;
            initial.Init().Wait();
            return "ok";
        }

        private static IConfigurationBuilder ConfigurationBuilder(string environment) => new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile(_appsettings, optional: true)
                        .AddJsonFile($"appsettings.{environment}.json", optional: true);

        private static IServiceCollection ConfigureServices(IConfiguration config) => new ServiceCollection()
                        .AddLogging()
                        .AddLogging(logging =>
                        {
                            logging.AddConfiguration(config.GetSection("Logging"));
                            logging.AddConsole();
                        })
                        // .AddDefaultAWSOptions(config.GetAWSOptions())
                        // .AddAWSService<IAmazonS3>()
                        .AddDbContext<ArchivoDBContext>(x =>
                            x.UseSqlServer(config.GetConnectionString("Default")))
                        // Aplication Services
                        .AddSingleton<INotificacionesService, NotificacionesService>()
                        .AddSingleton<INotificacionesService>(x =>
                            new NotificacionesService(x.GetRequiredService<ILogger<NotificacionesService>>(),
                                x.GetRequiredService<IDBService>(),
                                envVariables))
                        .AddSingleton<IAWSService>(x =>
                            new AWSService(x.GetRequiredService<ILogger<AWSService>>(),
                                // x.GetRequiredService<IAmazonS3>(),
                                x.GetRequiredService<INotificacionesService>(),
                                envVariables))
                        .AddSingleton<IDBService>(x =>
                            new DBService(x.GetRequiredService<ILogger<DBService>>(),
                                x.GetRequiredService<ArchivoDBContext>(),// x.GetRequiredService<INotificacionesService>(),
                                envVariables))
                        .AddSingleton<IFileService>(x =>
                            new FileService(x.GetRequiredService<ILogger<FileService>>(),
                                x.GetRequiredService<INotificacionesService>(), x.GetRequiredService<IDBService>(),
                                x.GetRequiredService<IAWSService>(),
                                envVariables))
                        .AddSingleton<IIniciaProceso>(x =>
                            new IniciaProceso(x.GetRequiredService<ILogger<IniciaProceso>>(),
                                x.GetRequiredService<IAWSService>(), x.GetRequiredService<IDBService>(),
                                x.GetRequiredService<IFileService>(), envVariables));
    }
}