# Bonos de desempeño — Full Stack

Repositorio público con el código fuente del módulo web de bonos de desempeño desarrollado con Angular, ASP.NET Core y SQL Server.

## Tecnologías

- Angular 22 standalone, TypeScript y RxJS.
- ASP.NET Core Web API sobre .NET 10 y Entity Framework Core.
- SQL Server 2019 o superior.
- xUnit para pruebas unitarias del cálculo.

## Requisitos locales

- Windows 10/11 de 64 bits.
- .NET 10 SDK.
- Node.js 24.15+, 22.22.3+ o 26+ con npm.
- SQL Server local y una cadena de conexión válida en `backend/src/BonusTest.Api/appsettings.json`.

Angular 22 requiere una versión de Node.js que cumpla los mínimos anteriores.

## Preparación

El repositorio público no contiene el PDF ni el script de datos iniciales entregados con la evaluación. En una base nueva, ejecuta el script inicial del paquete original y luego `database/02_create_bonus.sql` para crear la tabla e índices del módulo.

## Iniciar en Windows

1. Ejecuta `INSTALAR_DEPENDENCIAS.bat`.
2. Prepara la base de datos como se indica arriba.
3. Ejecuta `INICIAR_APLICACION.bat`; el lanzador espera las respuestas de la API y Angular y abre el navegador.
4. Ejecuta `DETENER_APLICACION.bat` para apagar los procesos iniciados.

La API usa `http://localhost:5106` y Angular `http://localhost:4200`. Los logs quedan en `.run/logs`.

Se entregan lanzadores `.bat` y scripts PowerShell. No se incluye un ejecutable `.exe`; no se pudo compilar y verificar uno en el entorno de preparación.

## Funcionalidades

- Búsqueda de empleado y validación de estado.
- Registro de bonos fijos o porcentuales.
- Cálculo de bruto, retención y neto.
- Listado filtrado y paginado.
- Anulación y eliminación lógica con confirmación.
- Registro de auditoría mediante el header `UserCode`.

## Pruebas

Los casos unitarios están en `backend/tests/BonusTest.Tests`. Para ejecutarlos con .NET 10 SDK:

`dotnet test .\backend\BonusTest.slnx`

El reporte de verificación del entorno de preparación está separado del código público.