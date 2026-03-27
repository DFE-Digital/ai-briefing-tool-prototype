using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using BriefingTool.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace BriefingTool.Pages
{
    [Authorize]
    public class IndexModel(
        ILogger<IndexModel> logger,
        IBriefingRunner runner ) : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Enter an academy name")]
        [Display(Name = "AcademyName")]
        public string? AcademyName { get; set; }

        [BindProperty]
        [Display(Name = "Result")]
        public string? Result { get; set; }

        [BindProperty]
        [Display(Name = "Ofsted")]
        public bool Ofsted { get; set; }

        [BindProperty]
        [Display(Name = "Concerns")]
        public bool Concerns { get; set; }

        [BindProperty]
        [Display(Name = "Financial")]
        public bool Financial { get; set; }

        [BindProperty]
        [Display(Name = "Additional prompt information")]
        public string? AdditionalPrompt { get; set; }

        [BindProperty]
        [Display(Name = "Debug")]
        public bool DebugOutput { get; set; }

        [BindProperty]
        [Display(Name = "Debug information about prompt")]
        public string? DebugPrompt { get; set; }

        [BindProperty] public IFormFile? UploadFile { get; set; }
        
        public void OnGet()
        {

        }

        [Experimental("AOAI001")]
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string? fileContents = null;

            if (UploadFile != null)
            {
                fileContents = await ConvertFile(UploadFile);
            }

            var output = await runner.GetBriefing(new BriefingParameters
            (
                AcademyName ?? "",
                Ofsted,
                Concerns,
                Financial,
                AdditionalPrompt,
                fileContents
            ));

            //if(output.)

            Result = output.output;
            DebugPrompt = output.debug;

            return Page();
        }


        private async Task<string> ConvertFile(IFormFile uploadFile)
        {
            await using var stream = uploadFile.OpenReadStream();

            var filecontents = WordToHtmlConverter.ConvertDocxToHtml(stream);

            filecontents = Regex.Replace(filecontents, "<style.*?>.*?</style>", "", RegexOptions.Singleline);

            filecontents = Regex.Replace(filecontents, "style=['\"].*?['\"]", "", RegexOptions.IgnoreCase);

            filecontents = Regex.Replace(filecontents, "class=['\"].*?['\"]", "", RegexOptions.IgnoreCase);
            return filecontents;
        }
    }
}
