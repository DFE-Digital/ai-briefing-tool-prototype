using BriefingTool.Constants;
using BriefingTool.Models;
using BriefingTool.Runners.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BriefingTool.Pages;

[Authorize]
public class BriefingModel(ILogger<BriefingModel> logger, IDatabricksQueryBriefingRunner databricksQueryBriefingRunner) : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "Enter a user prompt for briefing")]
    [Display(Name = "Prompt")]
    public string Prompt { get; set; } = string.Empty;

    [BindProperty]
    [Display(Name = "Result")]
    public string? Result { get; set; }

    public IActionResult OnGet()
    {
        ViewData[TabNavigationModel.ViewDataKey] = new TabNavigationModel(TabNavigationModel.DatabricksQuery);
        return Page();
    }

    [Experimental("AOAI001")]
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
         

        var output = await databricksQueryBriefingRunner.GetBriefing(new DatabricksQueryBriefingParameters(Prompt));

        Result = output.Output;

        logger.LogInformation("Brieifing Result: {Result}", Result);

        return Page();
    }
}
