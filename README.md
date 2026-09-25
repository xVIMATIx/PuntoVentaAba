# PuntoAba

Sistema de punto de venta para una tienda de abarrotes, desarrollado con ASP.NET Core 9, Blazor Server, Entity Framework Core y SQLite.

## Funciones principales

- Punto de venta y registro de ventas.
- Productos, categorias, clientes y usuarios.
- Inventario y movimientos de existencias.
- Reportes y panel de control.
- API REST con autenticacion JWT.
- Integracion con balanza mediante puerto serie.
- Soporte para productos vendidos por peso.

## Requisitos

- .NET 9 SDK.

## Ejecutar el proyecto

```powershell
cd PuntoAba/PuntoAba
dotnet restore
dotnet run
```

En desarrollo, la aplicacion crea o actualiza una base de datos SQLite local. Las bases de datos, archivos temporales y resultados de compilacion no se incluyen en Git.

## Documentacion

- [Documentacion tecnica](README_TECNICO.md)
- [Manual tecnico](ManualTecnico.md)

## Seguridad

La configuracion incluida es solamente para desarrollo. Antes de usar el sistema en produccion, cambie la clave JWT y la contrasena inicial del administrador mediante configuracion segura.
