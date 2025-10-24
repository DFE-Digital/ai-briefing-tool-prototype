using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BriefingTool.Pages
{
    public class AdminModel(IConfiguration configuration) : PageModel
    {

        [BindProperty]
        [Display(Name = "Output")]
        public string? Output { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {

            var apiKey = configuration["AZURE_SEARCH_KEY"];
            if (string.IsNullOrEmpty(apiKey))
            {

                Output = "Please set the AZURE_SEARCH_KEY environment variable.";
            }

            var endpoint = "https://ofstedsearch.search.windows.net";

            var indexName = "my-sample-index";

            var credential = new AzureKeyCredential(apiKey);
            var indexClient = new SearchIndexClient(new Uri(endpoint), credential);


            var index = new SearchIndex(indexName)
            {
                Fields = new List<SearchField>
                {
                    new SimpleField("URN", SearchFieldDataType.String) { IsKey = true },
                    new SimpleField("LAESTAB", SearchFieldDataType.String),
                    new SearchableField("School name") { IsFilterable = true, IsSortable = true },
                    new SimpleField("Ofsted phase", SearchFieldDataType.String),
                    new SimpleField("Type of education", SearchFieldDataType.String),
                    new SimpleField("School open date", SearchFieldDataType.String),
                    new SimpleField("Admissions policy", SearchFieldDataType.String),
                    new SimpleField("Sixth form", SearchFieldDataType.String),
                    new SimpleField("Designated religious character", SearchFieldDataType.String),
                    new SimpleField("Religious ethos", SearchFieldDataType.String),
                    new SimpleField("Faith grouping", SearchFieldDataType.String),
                    new SimpleField("Ofsted region", SearchFieldDataType.String),
                    new SimpleField("Region", SearchFieldDataType.String),
                    new SimpleField("Local authority", SearchFieldDataType.String),
                    new SimpleField("Parliamentary constituency", SearchFieldDataType.String),
                    new SimpleField("Multi-academy trust UID", SearchFieldDataType.String),
                    new SimpleField("Multi-academy trust name", SearchFieldDataType.String),
                    new SimpleField("Academy sponsor UID", SearchFieldDataType.String),
                    new SimpleField("Academy sponsor name", SearchFieldDataType.String),
                    new SimpleField("Postcode", SearchFieldDataType.String),
                    new SimpleField("The income deprivation affecting children index (IDACI) quintile", SearchFieldDataType.String),
                    new SimpleField("Total number of pupils", SearchFieldDataType.Int32),
                    new SimpleField("Statutory lowest age", SearchFieldDataType.Int32),
                    new SimpleField("Statutory highest age", SearchFieldDataType.Int32),
                    new SimpleField("Inspection number", SearchFieldDataType.String),
                    new SimpleField("Inspection type", SearchFieldDataType.String),
                    new SimpleField("Inspection type grouping", SearchFieldDataType.String),
                    new SimpleField("Event type grouping", SearchFieldDataType.String),
                    new SimpleField("Inspection start date", SearchFieldDataType.String),
                    new SimpleField("Publication date", SearchFieldDataType.String),
                    new SimpleField("Did the latest ungraded inspection convert to a graded inspection?", SearchFieldDataType.String),
                    new SimpleField("Outcomes for ungraded and monitoring inspections", SearchFieldDataType.String),
                    new SimpleField("Category of concern", SearchFieldDataType.String),
                    new SimpleField("Quality of education", SearchFieldDataType.Int32),
                    new SimpleField("Behaviour and attitudes", SearchFieldDataType.Int32),
                    new SimpleField("Personal development", SearchFieldDataType.Int32),
                    new SimpleField("Effectiveness of leadership and management", SearchFieldDataType.Int32),
                    new SimpleField("Safeguarding is effective", SearchFieldDataType.String),
                    new SimpleField("Early years provision (where applicable)", SearchFieldDataType.Int32),
                    new SimpleField("Sixth form provision (where applicable)", SearchFieldDataType.Int32),
                    new SimpleField("Previous inspection number", SearchFieldDataType.String),
                    new SimpleField("Previous inspection start date", SearchFieldDataType.String),
                    new SimpleField("Previous publication date", SearchFieldDataType.String),
                    new SimpleField("Does the previous inspection relate to the school in its current form?", SearchFieldDataType.String),
                    new SimpleField("URN at time of previous inspection", SearchFieldDataType.String),
                    new SimpleField("LAESTAB at time of previous inspection", SearchFieldDataType.String),
                    new SimpleField("School name at time of previous inspection", SearchFieldDataType.String),
                    new SimpleField("School type at time of previous inspection", SearchFieldDataType.String),
                    new SimpleField("Previous overall effectiveness", SearchFieldDataType.Int32),
                    new SimpleField("Previous category of concern", SearchFieldDataType.String),
                    new SimpleField("Previous quality of education", SearchFieldDataType.Int32),
                    new SimpleField("Previous behaviour and attitudes", SearchFieldDataType.Int32),
                    new SimpleField("Previous personal development", SearchFieldDataType.Int32),
                    new SimpleField("Previous effectiveness of leadership and management", SearchFieldDataType.Int32),
                    new SimpleField("Previous safeguarding is effective?", SearchFieldDataType.String),
                    new SimpleField("Previous early years provision (where applicable)", SearchFieldDataType.Int32),
                    new SimpleField("Previous sixth form provision (where applicable)", SearchFieldDataType.Int32)
                }
            };

            indexClient.CreateOrUpdateIndex(index);
            Output = $"Index '{indexName}' created successfully.";

            return Page();
        }
    }
}
