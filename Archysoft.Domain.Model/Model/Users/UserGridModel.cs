﻿using System;

namespace Archysoft.Domain.Model.Model.Users
{
    public class UserGridModel
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public byte[] Photo { get; set; }
    }
}
