# Prueba técnica Full Stack — Bonos de desempeño

Solución de la prueba técnica para la Clínica de Especialidades Médicas. El proyecto parte del código entregado en el examen y completa el módulo para consultar y registrar bonos de empleados.

## Tecnologías

- SQL Server 2019 o superior.
- ASP.NET Core Web API sobre .NET 10 y Entity Framework Core para SQL Server.
- Angular 22 standalone, TypeScript y RxJS.
- xUnit para pruebas unitarias del cálculo monetario.

## Requisitos para ejecutar

- Windows 10/11 de 64 bits.
- .NET 10 SDK.
- Node.js 24.15 o superior, 22.22.3 o superior, o 26 o superior; npm incluido.
- Instancia local de SQL Server con autenticación de Windows, o una cadena de conexión ajustada en `backend/src/BonusTest.Api/appsettings.json`.

Angular 22 no admite Node 22.20.0; usa una versión que cumpla los mínimos indicados arriba.

## Preparar la base de datos

Con SQL Server iniciado, ejecuta los scripts en este orden desde una consola con `sqlcmd` o desde SQL Server Management Studio:

```powershell
sqlcmd -S localhost -E -i .\database\01_schema_seed.sql
sqlcmd -S localhost -E -i .\database\02_create_bonus.sql
```

El primer script crea la base `BonusTest`, su esquema base y los datos de demostración. Ejecútalo una sola vez en una base nueva. El segundo crea la tabla de bonos, sus relaciones, validaciones e índices.

La conexión predeterminada es `Server=localhost;Database=BonusTest;Trusted_Connection=True;TrustServerCertificate=True;`. Para usar otra instancia o autenticación, modifica la cadena `ConnectionStrings:BonusTest` antes de iniciar la API.

## Instalar e iniciar en Windows

1. Ejecuta `INSTALAR_DEPENDENCIAS.bat`. El script verifica Node.js y .NET 10, instala Angular desde `package-lock.json` y restaura los paquetes de .NET.
2. Ejecuta los dos scripts SQL descritos arriba.
3. Ejecuta `INICIAR_APLICACION.bat`. El lanzador inicia la API y Angular, espera respuestas de los servicios y abre la página en el navegador predeterminado.
4. Ejecuta `DETENER_APLICACION.bat` para detener los procesos iniciados por el lanzador.

La aplicación utiliza `http://localhost:5106` para la API y `http://localhost:4200` para Angular. Los archivos de salida del lanzador se guardan en `.run/logs`.

Los lanzadores entregados son archivos `.bat` y scripts PowerShell. No se incluye un `.exe`: no se pudo compilar y verificar uno nativo porque el entorno de desarrollo disponible no tiene el SDK .NET 10.

## Funcionalidades

- Búsqueda de empleados por área y número; solo se aceptan empleados activos.
- Registro de bono fijo o porcentual para un periodo y motivo activo.
- Cálculo de bruto, retención y neto con decimales y redondeo monetario a dos posiciones.
- Listado con filtros por área, empleado, periodo y estado, paginado en SQL Server.
- Anulación y eliminación lógica con confirmación; ambas acciones solo se habilitan para bonos registrados.
- Auditoría de usuario y fecha de creación o modificación mediante el header `UserCode`.
- Mensajes de validación de la API presentados por el interceptor Angular.

El pago y la distribución proporcional por proyecto son un punto opcional del examen y no forman parte de esta entrega.

## Endpoints

| Método | Ruta | Uso |
| --- | --- | --- |
| `GET` | `/api/v1/Bonus` | Listado paginado y filtros. |
| `GET` | `/api/v1/Bonus/employee/{departmentCode}/{employeeNumber}` | Buscar empleado elegible. |
| `POST` | `/api/v1/Bonus` | Registrar un bono. |
| `PUT` | `/api/v1/Bonus/{bonusId}/cancel` | Anular un bono registrado. |
| `DELETE` | `/api/v1/Bonus/{bonusId}` | Eliminar lógicamente un bono registrado. |

Ejemplo de alta:

```http
POST http://localhost:5106/api/v1/Bonus
Content-Type: application/json
UserCode: BRUNO.SALAZAR

{
  "departmentCode": "TECH1",
  "employeeNumber": 101,
  "period": "2026-11",
  "bonusTypeId": 2,
  "value": 10,
  "reasonCode": "000001"
}
```

El empleado de demostración TECH1-101 tiene sueldo mensual de S/ 5,000 y retención de 8 %. Un bono de 10 % produce bruto de S/ 500.00, retención de S/ 40.00 y neto de S/ 460.00.

## Pruebas

Los casos unitarios están en `backend/tests/BonusTest.Tests`. Con .NET 10 SDK instalado, ejecuta:

```powershell
dotnet test .\backend\BonusTest.slnx
```

El estado de compilación y las verificaciones realizadas en el entorno de preparación están detallados en `REPORTE_PRUEBAS.md`. No se marca como exitosa ninguna prueba que no haya sido ejecutada.

## Documentos de entrega

- `ANALISIS_EXAMEN.md`: requisitos y plan derivados del examen original.
- `REPORTE_TECNICO.md`: arquitectura, tecnologías, funcionalidades y decisiones.
- `REPORTE_PRUEBAS.md`: pruebas realizadas, bloqueos y limitaciones.
- `database/`: script semilla original y script nuevo para bonos.
