using System.Security.Cryptography;
using System.Text;
using DatingApp.API.Data.Entities;
using DatingApp.API.DTOs;
using DatingApp.API.Services;
using DatingApp.DatingApp.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
 
namespace DatingApp.API.Controllers
{
    public class AuthController : BaseController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        public AuthController(DataContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }
        //
        // user register
        //
        [HttpPost("UserRegister")]
        public IActionResult Register([FromBody] AuthUserDto authUserDto)
        {
            authUserDto.Username = authUserDto.Username.ToLower();
            if (_context.Users.Any(u => u.Username == authUserDto.Username))
            {
                return BadRequest("Username is already registered!");
            }
 
            using var hmac = new HMACSHA512();
            var passwordBytes = Encoding.UTF8.GetBytes(authUserDto.Password);
            var newUser = new User
            {
                Username = authUserDto.Username,
                Email = authUserDto.Email,
                PasswordSalt = hmac.Key,
                PasswordHash = hmac.ComputeHash(passwordBytes)
            };
            _context.Users.Add(newUser);
            _context.SaveChanges();
            var token = _tokenService.CreateToken(newUser.Username);
            return Ok(token);
        }
        //
        // login
        //
        [HttpPost("login")]
        public IActionResult Login([FromBody] AuthUserDto authUserDto)
        {
            authUserDto.Username = authUserDto.Username.ToLower();
            var currentUser = _context.Users
                .FirstOrDefault(u => u.Username == authUserDto.Username);
           
            if (currentUser == null)
            {
                return Unauthorized("Username is invalid.");
            }
 
            using var hmac = new HMACSHA512(currentUser.PasswordSalt);
            var passwordBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(authUserDto.Password));
            for (int i = 0; i < currentUser.PasswordHash.Length; i++)
            {
                if (currentUser.PasswordHash[i] != passwordBytes[i])
                {
                    return Unauthorized("Password is invalid.");
                }
            }
 
            var token = _tokenService.CreateToken(currentUser.Username);
            return Ok(token);
        }
        //
        // get list user
        //
        //[Authorize]
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_context.Users.ToList());
        }

    }
}
