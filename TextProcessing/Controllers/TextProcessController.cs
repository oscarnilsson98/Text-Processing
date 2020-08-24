using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TextProcessing.Models;
using Humanizer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace TextProcessing.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TextProcessController : ControllerBase
    {
        private IHostingEnvironment _hostingEnvironment;

        public TextProcessController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult Post()
        {
            try
            {
                //Gets the file
                var file = Request.Form.Files[0];

                //Creates the path to the folder
                string folderName = "Upload";
                string webRootPath = _hostingEnvironment.WebRootPath;
                string newPath = Path.Combine(webRootPath, folderName);
                //Check if the folder already exists
                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                }

                //Checks if the file is empty
                if (file.Length > 0)
                {
                    //Gets the file name
                    string fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.ToString();

                    //Creates the full path to the file
                    string fullPath = Path.Combine(newPath, fileName);

                    //Sparar filen med texten 
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    //Create Lists which are needed for manipulation
                    List<Text> fulltext = new List<Text>();
                    List<string> fulltextstring = new List<string>();
                    List<string> editedtext = new List<string>();

                    //Goes through  all lines in the text file and adds it to two lists
                    //One list is used to get all the distinct words with their counts => taken away  all punctuations and empty strings
                    //One list has the text into every word divided by line
                    foreach (var line in System.IO.File.ReadLines(fullPath))
                    {
                        var editline = line.ToLower();
                        editline = editline.Replace("_ ", "_");
                        editline = new string(editline.Where(c => !char.IsPunctuation(c)).ToArray());
                        var editwords = editline.Split();
                        editwords = editwords.Where(x => x != "").ToArray();
                        //Using the Humanizer nuget to check if a word is plural and then making it into singular -- Taken away because the process time
                        //becomes much longer, especially with the large file
                        /*
                        for (int i = 0; i < editwords.Count(); i++)
                        {
                            editwords[i] = editwords[i].Singularize(inputIsKnownToBePlural: false);
                        }
                        */
                        fulltextstring.AddRange(editwords);


                        Text temptext = new Text();
                        var ol = line.Replace("_ ", "_");
                        temptext.Oneline = ol.Split();
                        fulltext.Add(temptext);
                    }

                    //Linq query to find all distinct words and how many times they are present in the text from the edited list with all words
                    var query = fulltextstring.GroupBy(x => x)
                        .Where(g => g.Count() > 1)
                        .Select(y => new { Element = y.Key, Counter = y.Count() })
                        .ToList();

                    //Gets the count of the most used word
                    int highestcount = query.Select(w => w.Counter).Max();

                    //Adds all the distinct words which are used the most in a List
                    List<string> mostpopularwords = query.Where(x => x.Counter == highestcount).Distinct().Select(s => s.Element).ToList();

                    //Processes the unedited text  and adds foo bar to the most popular words
                    for (int i = 0; i < fulltext.Count(); i++)
                    {
                        for (int e = 0; e < fulltext[i].Oneline.Count(); e++)
                        {
                            string check = fulltext[i].Oneline[e].ToLower();

                            check = new string(check.Where(c => !char.IsPunctuation(c)).ToArray());

                            //check = check.Singularize(inputIsKnownToBePlural: false);

                            if (mostpopularwords.Contains(check))
                            {
                                if (fulltext[i].Oneline[e].All(char.IsUpper))
                                {
                                    fulltext[i].Oneline[e] = "FOO" + fulltext[i].Oneline[e] + "BAR";
                                }
                                else if (char.IsUpper(fulltext[i].Oneline[e].First()))
                                {
                                    fulltext[i].Oneline[e] = "Foo" + fulltext[i].Oneline[e].ToLower() + "bar";
                                }
                                else
                                {
                                    fulltext[i].Oneline[e] = "foo" + fulltext[i].Oneline[e] + "bar";
                                }
                            }

                        }
                    }

                    //Combines the words back togethe into a list of lines
                    foreach (var ft in fulltext)
                    {
                        string combindedString = string.Join(" ", ft.Oneline.ToArray());
                        editedtext.Add(combindedString);
                    }

                    //Update the file with the processed text
                    System.IO.File.WriteAllLines(fullPath, editedtext);

                    //Gets a var with the file in Bytes
                    var dataBytes = System.IO.File.ReadAllBytes(fullPath);

                    //Deletes the saved file 
                    System.IO.File.Delete(fullPath);

                    //Return the file
                    return new FileContentResult(dataBytes, new
                        MediaTypeHeaderValue("application/octet"))
                    {
                        FileDownloadName = fileName
                    };
                }
                else
                {
                    //Return Not Found if file is empty
                    return NotFound();
                }
            }
            catch (System.Exception ex)
            {
                //Return bad request with exception if try did not work 
                return BadRequest(ex);
            }
        }
    }
}