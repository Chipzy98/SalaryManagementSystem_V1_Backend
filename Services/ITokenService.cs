using SalaryManagementAPI.Models;

namespace SalaryManagementAPI.Services
{
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}
