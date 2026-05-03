using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdbClient.Data.Data;

public class UserData(DataContext dataContext)
{
    public async Task<IdentityUser?> GetUser()
    {
        return await dataContext.Users.OrderBy(m => m.Id).FirstOrDefaultAsync();
    }
}