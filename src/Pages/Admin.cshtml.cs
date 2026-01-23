using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using BriefingTool.Services;
using Microsoft.AspNetCore.Authorization;

namespace BriefingTool.Pages
{
    [Authorize]
    public class AdminModel(IConfiguration configuration,
        IAcademyInformationRetriever academyInformationRetriever,
        IOfstedIndexer ofstedIndexer) : PageModel
    {

        [BindProperty]
        [Display(Name = "Output")]
        public string? Output { get; set; }

        public void OnGet()
        {
        }


        public async Task<IActionResult> OnPostAsync()
        {

            try
            {
                await ofstedIndexer.CreateIndex();
                Output = "Index created";
            }
            catch(Exception ex)
            {
                Output = $"Error creating index: {ex.Message}";
            }


            return Page();
        }
    }
}
