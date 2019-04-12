using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using SunEngine.Commons.Cache;
using SunEngine.Commons.Cache.CacheModels;

namespace SunEngine.Commons.Security
{
    public class MyClaimsPrincipal : ClaimsPrincipal
    {
        public int UserId { get; }
        public long SessionId { get; }
        public string LongToken2Db { get; }

        public IReadOnlyDictionary<string, RoleCached> Roles { get; }
        
        /// <summary>
        /// If only one group
        /// </summary>
        public RoleCached Role { get; }

        public MyClaimsPrincipal(ClaimsPrincipal user, IRolesCache rolesCache, long sessionId = 0, string longToken2Db = null) : base(user)
        {
            this.SessionId = sessionId;
            this.LongToken2Db = longToken2Db;
            
            if (Identity.IsAuthenticated)
                UserId = int.Parse(this.FindFirstValue(ClaimTypes.NameIdentifier));
            
            Roles = GetUserRoles(rolesCache);
            if (Roles.Count == 1)
                Role = Roles.Values.ElementAt(0);
        }
        
        private IReadOnlyDictionary<string, RoleCached> GetUserRoles(IRolesCache rolesCache)
        {
            if (!Identity.IsAuthenticated)
            {
                return new Dictionary<string, RoleCached>
                {
                    [RoleNames.Unregistered] = rolesCache.GetRole(RoleNames.Unregistered)
                }.ToImmutableDictionary();
            }

            var roles = GetRolesNames();
            var allGroups = rolesCache.AllRoles;


            var dictionaryBuilder = ImmutableDictionary.CreateBuilder<string,RoleCached>();

            var registeredGroup = rolesCache.GetRole(RoleNames.Registered);
            dictionaryBuilder.Add(registeredGroup.Name, registeredGroup);
            foreach (var role in roles)
            {
                if (!allGroups.ContainsKey(role)) continue;

                var userGroup = allGroups[role];
                dictionaryBuilder.Add(userGroup.Name, userGroup);
            }

            return dictionaryBuilder.ToImmutable();
        }

        private IEnumerable<string> GetRolesNames()
        {
            return Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).ToImmutableList();
        }
    }
}