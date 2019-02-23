﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using Liyanjie.Membership.Core;

namespace Liyanjie.Membership.AspNet.Mvc.ActionPath
{
    /// <summary>
    /// 
    /// </summary>
    public class AuthorityProvider : IAuthorityProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="assemblies"></param>
        /// <param name="options"></param>
        public AuthorityProvider(string[] assemblies, AuthorityOptions<AuthorityProvider, Type> options)
        {
            this.assemblies = assemblies ?? new string[0];
            this.options = options;
        }

        readonly string[] assemblies;
        readonly AuthorityOptions<AuthorityProvider, Type> options;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<IAuthority> GetAuthorities()
        {
            var authorities = new List<Authority>();
            foreach (var assembly in assemblies.Select(_ => Assembly.Load(_)))
            {
                var controllers = assembly.GetExportedTypes().Where(_ => _.IsSubclassOf(typeof(Controller))).ToList();
                if (options?.Filter != null)
                    controllers = controllers.Where(_ => options.Filter(_)).ToList();
                if (controllers.Count == 0)
                    continue;

                foreach (var controller in controllers)
                {
                    if (controller.GetCustomAttributes<AllowAnonymousAttribute>(true).Any())
                        continue;

                    var group = string.Join("\\", controller.GetCustomAttributes<AuthorityGroupAttribute>(true).Reverse().Select(_ => _.GroupName));

                    var actions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                        .Where(_ => !_.Name.StartsWith("get_", StringComparison.InvariantCultureIgnoreCase) && !_.Name.StartsWith("set_", StringComparison.InvariantCultureIgnoreCase))
                        .Distinct()
                        .ToList();
                    if (actions.Count == 0)
                        continue;

                    foreach (var action in actions)
                    {
                        if (action.GetCustomAttributes<AllowAnonymousAttribute>(true).Any())
                            continue;

                        var authority = action.GetCustomAttributes<AuthorityAttribute>().FirstOrDefault();

                        var resource = $"{controller.FullName}.{action.Name}";
                        var dependences = authority?.Dependences
                            .Select(_ => _.StartsWith("Controllers.") ? resource.Remove(resource.IndexOf("Controllers.")) + _ : _.StartsWith(".") ? resource.Remove(resource.LastIndexOf(".")) + _ : string.Empty)
                            .ToArray();
                        var title = authority?.Title;

                        var exists = authorities.FirstOrDefault(_ => _.Resource == resource);
                        if (exists == null)
                            authorities.Add(new Authority
                            {
                                Group = group,
                                Resource = resource,
                                Dependences = dependences,
                                Title = title,
                            });
                        else
                        {
                            if (exists.Title == null)
                                exists.Title = title;
                            if (exists.Dependences == null)
                                exists.Dependences = dependences;
                        }
                    }
                }
            }

            return authorities;
        }
    }
}
