using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private User? _currentUser;

        public AuthService()
        {
            _context = new ApplicationDbContext();
        }

        public User? CurrentUser => _currentUser;

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        public bool Register(string username, string email, string password)
        {
            if (_context.Users.Any(u => u.Email == email))
            {
                return false;
            }

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password)
            };

            _context.Users.Add(user);
            _context.SaveChanges();
            return true;
        }

        public bool Login(string email, string password)
        {
            var hash = HashPassword(password);
            var user = _context.Users
                .Include(u => u.Organization)
                .FirstOrDefault(u => u.Email == email && u.PasswordHash == hash);

            if (user != null)
            {
                _currentUser = user;
                return true;
            }
            return false;
        }

        public void Logout()
        {
            _currentUser = null;
        }

        public void RefreshCurrentUser()
        {
            if (_currentUser == null)
            {
                return;
            }

            var id = _currentUser.Id;
            _context.ChangeTracker.Clear();
            var user = _context.Users
                .AsNoTracking()
                .Include(u => u.Organization)
                .FirstOrDefault(u => u.Id == id);

            if (user != null)
            {
                _currentUser = user;
            }
        }
    }
}