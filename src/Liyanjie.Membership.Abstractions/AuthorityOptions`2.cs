﻿using System;

namespace Liyanjie.Membership
{
    /// <summary>
    /// 
    /// </summary>
    public class AuthorityOptions<TAuthorityProvider, TAuthorityDescriptor>
    {
        /// <summary>
        /// 
        /// </summary>
        public Func<TAuthorityDescriptor, bool> Filter { get; set; }
    }
}
