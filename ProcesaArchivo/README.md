# Procesa Archivo

El proceso se encarga de descargar un archivo desde un bucket de S3, para descomprimirlo en caso de que sae un .ZIP, y posteriornmente dividirlo en pequeñas partes para subirlo a una carpeta definida en TB_Archivo.

## Tecnología del proceso

El proceso esta diseñado en una aplicación de consola, usando tecnologia .NET Core 3.0. La aplicación esta diseñada para correr dentro de un container y funcionar como lambda de AWS (custom runtime). [Aquí](https://aws.amazon.com/blogs/developer/net-core-3-0-on-lambda-with-aws-lambdas-custom-runtime/) un ejemplo de AWS.

## Estructura del proyecto

```TREE
ProcesaArchivo
├─ .vscode
│  └─ launch.json
├─ src
│  └─ ProcesaArchivo
│     ├─ appsettings.dev.json
│     ├─ appsettings.json
│     ├─ appsettings.qa.json
│     ├─ aws-lambda-tools-defaults.json
│     ├─ event.json
│     ├─ IniciaProceso.cs
│     ├─ Models
│     │  ├─ Archivo.cs
│     │  ├─ ArchivoDBContext.cs
│     │  ├─ ArchivoInstancia.cs
│     │  ├─ ArchivoInterno.cs
│     │  ├─ ControlCarga.cs
│     │  ├─ ControlDescarga.cs
│     │  ├─ Enummerator.cs
│     │  ├─ EnvVariables.cs
│     │  ├─ FormatosFechas.cs
│     │  ├─ Notificacion.cs
│     │  └─ OptionsParse.cs
│     ├─ ProcesaArchivo.csproj
│     ├─ Program.cs
│     ├─ README.md
│     └─ Services
│        ├─ AWSService.cs
│        ├─ DBService.cs
│        ├─ FileService.cs
│        └─ NotificacionesService.cs
└─ test
   └─ ProcesaArchivo.Tests
      ├─ event.json
      └─ FunctionTest.cs
```

### Repositorio

El repo esta alojado en Amazon CodeCommit, especificamente en la cuenta de desarrollo.

## Implementar cambios en la app.

Para poder implementar cambios en la app, asegurate de trabajar en una rama independiente (que no sea **master**, **testing** ni **desarrollo**) y solo subir tus cambios a través de un _merge_ con **desarrollo**.

## Debug

Para debuguear la lambda se necesita VSCode y configurar el archivo .vscode/launch.json, asegurate que el archivo contenga la siguiente configuración:

```JSON
{
   "version": "0.2.0",
   "configurations": [
       {
            "name": ".NET Core Launch (console)",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/src/ProcesaArchivo/bin/Debug/netcoreapp3.0/ProcesaArchivo.dll",
            "args": [],
            "env": {
                "ENVIRONMENT": "dev",
                "URL_NOTIFICACIONES": "https://example.com/get",
                "BUCKET_S3": "name_bucket",
                "PREFIJO_CARPETA_S3": "pre",
                "NUMERO_REGISTROS_POR_PARTE": "10000",
                "ACCESS_KEY": "XXXXX",
                "SECRET_KEY": "YYYYYYYYY/S0T"
            },
            "cwd": "${workspaceFolder}/src/ProcesaArchivo",
            "console": "integratedTerminal",
            "stopAtEntry": false
        }
    ]
}
```
