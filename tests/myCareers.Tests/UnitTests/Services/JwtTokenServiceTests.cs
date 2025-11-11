using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using myCareers.Core.Entities;
using Microsoft.Extensions.Configuration;

public class JwtTokenService
{
	private readonly IConfiguration _configuration;

	public JwtTokenService(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public string GenerateAccessToken(User user)
	{
		var secretKey = new SymmetricSecurityKey(
			Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));
		var creds = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

		var claims = new[]
		{
			new Claim("userId", user.Id.ToString()),
			new Claim(ClaimTypes.Email, user.Email),
			new Claim(ClaimTypes.Role, user.Role.ToString())
		};

		var token = new JwtSecurityToken(
			issuer: _configuration["JWT:Issuer"],
			audience: _configuration["JWT:Audience"],
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JWT:ExpireMinutes"])),
			signingCredentials: creds);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	public int GetUserIdFromToken(string token)
	{
		if (string.IsNullOrEmpty(token))
			return 0;

		try
		{
			var handler = new JwtSecurityTokenHandler();
			var jwtToken = handler.ReadJwtToken(token);
			var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId");
			return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
		}
		catch
		{
			return 0;
		}
	}

	public string GenerateRefreshToken()
	{
		return Guid.NewGuid().ToString().Replace("-", "") + Guid.NewGuid().ToString().Replace("-", "");
	}

	public bool ValidateToken(string token)
	{
		if (string.IsNullOrEmpty(token))
			return false;

		var tokenHandler = new JwtSecurityTokenHandler();
		var key = Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]);
		try
		{
			tokenHandler.ValidateToken(token, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidIssuer = _configuration["JWT:Issuer"],
				ValidAudience = _configuration["JWT:Audience"],
				ClockSkew = TimeSpan.Zero
			}, out _);

			return true;
		}
		catch
		{
			return false;
		}
	}
}
