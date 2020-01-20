#!/bin/bash

dotnet restore ProcesaArchivo/src/ProcesaArchivo/ProcesaArchivo.csproj
dotnet lambda package --project-location ProcesaArchivo/src/ProcesaArchivo --configuration release --framework netcoreapp3.0 --output-package ProcesaArchivo/src/ProcesaArchivo/bin/release/netcoreapp3.0/ProcesaArchivo.zip

dotnet restore ValidaLayout/src/ValidaLayout/ValidaLayout.csproj
dotnet lambda package --project-location ValidaLayout/src/ValidaLayout --configuration release --framework netcoreapp3.0 --output-package ValidaLayout/src/ValidaLayout/bin/release/netcoreapp3.0/ValidaLayout.zip
