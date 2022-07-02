﻿using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors; // TODO Remember to remove cors and all EnableCors attributes
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using Users.Dtos;
using Users.Entities;
using Users.Repositories;
using Users.Settings;

namespace Users.Controllers
{
    // [ApiKeyAuth]
    [ApiController]
    [EnableCors("AllowOrigin")]
    [Route("api/[controller]")]
    public class UserLoginController : ControllerBase
    {
        private readonly IUsersRepository repository;

        public UserLoginController(IUsersRepository repository)
        {
            this.repository = repository;
        }

        [AllowAnonymous]
        [EnableCors("AllowOrigin")]
        [HttpPost]
        public ActionResult Login(LoginUserDto userDto)
        {
            var user = repository.GetUser(userDto.Email);
            if (user is null) return NotFound();

            if (Hash.CompareHashes(userDto.Password, user.Password))
                return Ok(
                    new
                    {
                        user.Id,
                        user.FirstName,
                        user.LastName,
                        user.Email,
                        user.CreatedDate,
                        token = Generate(user),
                    });
            return NotFound("User Not Found");
        }

        private string Generate(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Jwt.Key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Email, user.Email),
            };
            
            var token = new JwtSecurityToken(
                Jwt.Issuer,
                Jwt.Audience,
                claims,
                expires: DateTime.Now.AddHours(5),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}