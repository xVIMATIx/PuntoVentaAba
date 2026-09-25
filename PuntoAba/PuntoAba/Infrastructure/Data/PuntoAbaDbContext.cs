using Microsoft.EntityFrameworkCore;
using PuntoAba.Models;

namespace PuntoAba.Data
{
    public class PuntoAbaDbContext : DbContext
    {
        public PuntoAbaDbContext(DbContextOptions<PuntoAbaDbContext> options) : base(options)
        {
        }

        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetallesVenta { get; set; }
        public DbSet<MovimientoInventario> MovimientosInventario { get; set; }
        public DbSet<RegistroSincronizacion> RegistrosSincronizacion { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de índices
            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.Codigo)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.NombreUsuario)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Venta>()
                .HasIndex(v => v.NumeroVenta)
                .IsUnique();

            // Configuración de relaciones
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Cliente)
                .WithMany(c => c.Ventas)
                .HasForeignKey(v => v.ClienteId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Usuario)
                .WithMany(u => u.Ventas)
                .HasForeignKey(v => v.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetalleVenta>()
                .HasOne(d => d.Venta)
                .WithMany(v => v.DetallesVenta)
                .HasForeignKey(d => d.VentaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DetalleVenta>()
                .HasOne(d => d.Producto)
                .WithMany(p => p.DetallesVenta)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Producto)
                .WithMany(p => p.MovimientosInventario)
                .HasForeignKey(m => m.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Datos iniciales
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var fechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Categorías iniciales
            modelBuilder.Entity<Categoria>().HasData(
                new Categoria { Id = 1, Nombre = "Bebidas", Descripcion = "Bebidas y refrescos", FechaCreacion = fechaCreacion },
                new Categoria { Id = 2, Nombre = "Abarrotes", Descripcion = "Productos de abarrotes", FechaCreacion = fechaCreacion },
                new Categoria { Id = 3, Nombre = "Lácteos", Descripcion = "Productos lácteos", FechaCreacion = fechaCreacion },
                new Categoria { Id = 4, Nombre = "Panadería", Descripcion = "Pan y pastelería", FechaCreacion = fechaCreacion },
                new Categoria { Id = 5, Nombre = "Snacks", Descripcion = "Botanas y dulces", FechaCreacion = fechaCreacion },
                new Categoria { Id = 6, Nombre = "Limpieza", Descripcion = "Productos de limpieza", FechaCreacion = fechaCreacion }
            );

            // Usuario administrador inicial
            // Password: Admin123 (hasheado con BCrypt)
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1,
                    NombreUsuario = "admin",
                    NombreCompleto = "Administrador del Sistema",
                    Email = "admin@puntoaba.com",
                    PasswordHash = "$2a$11$9vJZ5qZ5qZ5qZ5qZ5qZ5qOxK3kZ4kZ4kZ4kZ4kZ4kZ4kZ4kZ4kZ4m",
                    Rol = "Admin",
                    Activo = true,
                    FechaCreacion = fechaCreacion
                }
            );

            // Cliente genérico
            modelBuilder.Entity<Cliente>().HasData(
                new Cliente
                {
                    Id = 1,
                    Nombre = "Cliente",
                    Apellido = "Genérico",
                    Telefono = "0000000000",
                    Email = "cliente@general.com",
                    Activo = true,
                    FechaCreacion = fechaCreacion
                }
            );
        }
    }
}
