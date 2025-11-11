using Microsoft.EntityFrameworkCore;
using myCareers.Core.Interfaces;
using myCareers.Core.Entities;
using myCareers.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using myCareers.Application.Interfaces;


namespace myCareers.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly myCareersDbContext _context;

        public UserRepository(myCareersDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Recruiter)
                .Include(u => u.Applicant)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Recruiter)
                .Include(u => u.Applicant)
                .FirstOrDefaultAsync(u => u.Email == email.ToLower());
        }

        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email.ToLower());
        }
    }
}
