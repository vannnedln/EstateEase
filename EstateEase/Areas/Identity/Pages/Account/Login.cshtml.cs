// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using EstateEase.Services;

namespace EstateEase.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly IReCaptchaService _recaptchaService;

        public LoginModel(
            SignInManager<IdentityUser> signInManager, 
            ILogger<LoginModel> logger,
            IConfiguration configuration,
            IReCaptchaService recaptchaService)
        {
            _signInManager = signInManager;
            _logger = logger;
            _configuration = configuration;
            _recaptchaService = recaptchaService;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }

            /// <summary>
            ///     reCAPTCHA response token
            /// </summary>
            [Required]
            public string RecaptchaResponse { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            try
            {
                // Verify reCAPTCHA first
                var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var recaptchaResult = await _recaptchaService.VerifyAsync(Input.RecaptchaResponse, remoteIp);
                
                if (!recaptchaResult.Success)
                {
                    _logger.LogWarning($"reCAPTCHA verification failed. Hostname: {recaptchaResult.Hostname}, " +
                                     $"Challenge Time: {recaptchaResult.ChallengeTimestamp}, " +
                                     $"Errors: {string.Join(", ", recaptchaResult.ErrorCodes ?? Array.Empty<string>())}");
                    
                    ModelState.AddModelError(string.Empty, "reCAPTCHA verification failed. Please try again.");
                    return Page();
                }

                // Log successful verification
                _logger.LogInformation($"reCAPTCHA verified successfully. Hostname: {recaptchaResult.Hostname}, " +
                                     $"Challenge Time: {recaptchaResult.ChallengeTimestamp}");

                // Only validate email/password if reCAPTCHA passes
                if (ModelState.IsValid)
                {
                    _logger.LogInformation("Attempting to sign in user");
                    var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                    
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User signed in successfully");
                        // Get the logged-in user
                        var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
                        // Check if user is in Admin role
                        if (await _signInManager.UserManager.IsInRoleAsync(user, "Admin"))
                        {
                            _logger.LogInformation("User is in Admin role, redirecting to Admin area");
                            return RedirectToAction("Index", "Home", new { area = "Admin" });
                        }
                        // Check if user is in Agent role
                        else if (await _signInManager.UserManager.IsInRoleAsync(user, "Agent"))
                        {
                            _logger.LogInformation("User is in Agent role, redirecting to Agent area");
                            return RedirectToAction("Index", "Home", new { area = "Agent" });
                        }

                        return LocalRedirect(returnUrl);
                    }
                    if (result.RequiresTwoFactor)
                    {
                        _logger.LogInformation("User requires two-factor authentication");
                        return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out.");
                        return RedirectToPage("./Lockout");
                    }
                    else
                    {
                        _logger.LogWarning("Invalid login attempt");
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        return Page();
                    }
                }
                else
                {
                    _logger.LogWarning($"Model state is invalid: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process");
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                return Page();
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
