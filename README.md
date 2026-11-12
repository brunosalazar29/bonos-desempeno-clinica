# Prueba t�cnica Full Stack - Bonos de desempe�o

Soluci�n de la prueba t�cnica para la Cl�nica de Especialidades M�dicas. El proyecto parte del c�digo entregado en el examen y completa el m�dulo para consultar y registrar bonos de empleados.

## Tecnolog�as

- SQL Server 2019 o superior.
- ASP.NET Core Web API sobre .NET 10 y Entity Framework Core para SQL Server.
- Angular 22 standalone, TypeScript y RxJS.
- xUnit para pruebas unitarias del c�lculo monetario.

## Requisitos para ejecutar

- Windows 10/11 de 64 bits.
- .NET 10 SDK.
- Node.js 24.15 o superior, 22.22.3 o superior, o 26 o superior; npm incluido.
- Instancia local de SQL Server con autenticaci�n de Windows, o una cadena de conexi�n ajustada en `backend/src/BonusTest.Api/appsettings.json`.

Angular 22 no admite Node 22.20.0; usa una versi�n que cumpla los m�nimos indicados arriba.

## Preparar la base de datos

Con SQL Server iniciado, ejecuta los scripts en este orden desde una consola con `sqlcmd` o desde SQL Server Management Studio:

```powershell
sqlcmd -S localhost -E -i .\database\01_schema_seed.sql
sqlcmd -S localhost -E -i .\database\02_create_bonus.sql
```

El primer script crea la base `BonusTest`, su esquema base y los datos de demostraci�n. Ejec�talo una sola vez en una base nueva. El segundo crea la tabla de bonos, sus relaciones, validaciones e �ndices.

La conexi�n predeterminada es `Server=localhost;Database=BonusTest;Trusted_Connection=True;TrustServerCertificate=True;`. Para usar otra instancia o autenticaci�n, modifica la cadena `ConnectionStrings:BonusTest` antes de iniciar la API.

## Instalar e iniciar en Windows

1. Ejecuta `INSTALAR_DEPENDENCIAS.bat`. El script verifica Node.js y .NET 10, instala Angular desde `package-lock.json` y restaura los paquetes de .NET.
2. Ejecuta los dos scripts SQL descritos arriba.
3. Ejecuta `INICIAR_APLICACION.bat`. El lanzador inicia la API y Angular, espera respuestas de los servicios y abre la p�gina en el navegador predeterminado.
4. Ejecuta `DETENER_APLICACION.bat` para detener los procesos iniciados por el lanzador.

La aplicaci�n utiliza `http://localhost:5106` para la API y `http://localhost:4200` para Angular. Los archivos de salida del lanzador se guardan en `.run/logs`.

Los lanzadores entregados son archivos `.bat` y scripts PowerShell. No se incluye un `.exe`: no se pudo compilar y verificar uno nativo porque el entorno de desarrollo disponible no tiene el SDK .NET 10.

## Funcionalidades

- B�squeda de empleados por �rea y n�mero; solo se aceptan empleados activos.
- Registro de bono fijo o porcentual para un periodo y motivo activo.
- C�lculo de bruto, retenci�n y neto con decimales y redondeo monetario a dos posiciones.
- Listado con filtros por �rea, empleado, periodo y estado, paginado en SQL Server.
- Anulaci�n y eliminaci�n l�gica con confirmaci�n; ambas acciones solo se habilitan para bonos registrados.
- Auditor�a de usuario y fecha de creaci�n o modificaci�n mediante el header `UserCode`.
- Mensajes de validaci�n de la API presentados por el interceptor Angular.

El pago y la distribuci�n proporcional por proyecto son un punto opcional del examen y no forman parte de esta entrega.

## Endpoints

| M�todo | Ruta | Uso |
| --- | --- | --- |
| `GET` | `/api/v1/Bonus` | Listado paginado y filtros. |
| `GET` | `/api/v1/Bonus/employee/{departmentCode}/{employeeNumber}` | Buscar empleado elegible. |
| `POST` | `/api/v1/Bonus` | Registrar un bono. |
| `PUT` | `/api/v1/Bonus/{bonusId}/cancel` | Anular un bono registrado. |
| `DELETE` | `/api/v1/Bonus/{bonusId}` | Eliminar l�gicamente un bono registrado. |

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

El empleado de demostraci�n TECH1-101 tiene sueldo mensual de S/ 5,000 y retenci�n de 8 %. Un bono de 10 % produce bruto de S/ 500.00, retenci�n de S/ 40.00 y neto de S/ 460.00.

## Pruebas

Los casos unitarios est�n en `backend/tests/BonusTest.Tests`. Con .NET 10 SDK instalado, ejecuta:

```powershell
dotnet test .\backend\BonusTest.slnx
```

El estado de compilaci�n y las verificaciones realizadas en el entorno de preparaci�n est�n detallados en `REPORTE_PRUEBAS.md`. No se marca como exitosa ninguna prueba que no haya sido ejecutada.

## Documentos de entrega

- `ANALISIS_EXAMEN.md`: requisitos y plan derivados del examen original.
- `REPORTE_TECNICO.md`: arquitectura, tecnolog�as, funcionalidades y decisiones.
- `REPORTE_PRUEBAS.md`: pruebas realizadas, bloqueos y limitaciones.
- `database/`: script semilla original y script nuevo para bonos.

