using System;
using System.Collections.Generic;

namespace NetHRSystem.Models
{
    public partial class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public byte[] PasswordHash { get; set; } = null!;
        public byte[] PasswordSalt { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}
