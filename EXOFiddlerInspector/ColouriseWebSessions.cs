﻿using System;
using System.Windows.Forms;
using Fiddler;
using System.Linq;

public class ColouriseWebSessions : IAutoTamper    // Ensure class is public, or Fiddler won't see it!
{
    //string sUserAgent = "";
    //private object fSessions;
    private bool bCreatedColumn = false;
    internal Session session { get; set; }

    //public object GetAllSessions { get ; private set; }

    //public Violin()
    //{
    /* NOTE: It's possible that Fiddler UI isn't fully loaded yet, so don't add any UI in the constructor.

       But it's also possible that AutoTamper* methods are called before OnLoad (below), so be
       sure any needed data structures are initialized to safe values here in this constructor */

    //    sUserAgent = "Violin";
    //}

    #region LoadSAZ
    /////////////////
    // 
    // Handle loading a SAZ file.
    //
    public void OnLoad()
    {
        FiddlerApplication.OnLoadSAZ += HandleLoadSaz;
    }

    private void HandleLoadSaz(object sender, FiddlerApplication.ReadSAZEventArgs e)
    {
        FiddlerApplication.UI.lvSessions.BeginUpdate();
        foreach (var session in e.arrSessions)
        {
            OnPeekAtResponseHeaders(session); //Run whatever function you use in IAutoTamper
            session.RefreshUI();
        }
        FiddlerApplication.UI.lvSessions.EndUpdate();
    }
    //
    /////////////////
    #endregion

    #region ColouriseRuleSet

    private void OnPeekAtResponseHeaders(Session session)
    {

        this.session = session;

        if (this.session.LocalProcess.Contains("outlook") ||
        this.session.LocalProcess.Contains("iexplore") ||
        this.session.LocalProcess.Contains("chrome") ||
        this.session.LocalProcess.Contains("firefox") ||
        this.session.LocalProcess.Contains("edge") ||
        this.session.LocalProcess.Contains("w3wp"))
        {

        

            int wordCount = 0;

            // Count the occurrences of common search terms match up to certain HTTP response codes to highlight certain scenarios.
            //
            // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/how-to-count-occurrences-of-a-word-in-a-string-linq
            //

            string text = this.session.ToString();

            //Convert the string into an array of words  
            string[] source = text.Split(new char[] { '.', '?', '!', ' ', ';', ':', ',' }, StringSplitOptions.RemoveEmptyEntries);

            //string searchTerm = "error";
            string[] searchTerms = { "Error", "FederatedStsUnreachable", "https://autodiscover-s.outlook.com/autodiscover/autodiscover.xml" };

            this.session.utilDecodeRequest(true);
            this.session.utilDecodeResponse(true);

            foreach (string searchTerm in searchTerms)
            {
                // Create the query.  Use ToLowerInvariant to match "data" and "Data"   
                var matchQuery = from word in source
                                 where word.ToLowerInvariant() == searchTerm.ToLowerInvariant()
                                 select word;

                // Count the matches, which executes the query.  
                wordCount = matchQuery.Count();

                switch (this.session.responseCode)
                {
                    case 200:
                        #region HTTP200
                        /////////////////////////////
                        //
                        // HTTP 200
                        //
                        // Looking for errors lurking in HTTP 200 OK responses.
                        if (searchTerm == "Error")
                        {
                            string result = "After splitting all words in the response body the word 'error' was found " + wordCount + " time(s).";

                            if (wordCount > 0)
                            {
                                this.session["ui-backcolor"] = "red";
                                this.session["ui-color"] = "black";
                            }
                            else
                            {
                                this.session["ui-backcolor"] = "green";
                                this.session["ui-color"] = "black";
                            }
                        }

                        // Autodiscover redirect Address from Exchange On-Premise.
                        if (session.utilFindInResponse("<RedirectAddr>", false) > 1)
                        {
                            if (session.utilFindInResponse("</RedirectAddr>", false) > 1)
                            {
                                this.session["ui-backcolor"] = "green";
                                this.session["ui-color"] = "black";
                            }
                        }
                        //
                        /////////////////////////////
                        #endregion
                        break;
                    case 302:
                        #region HTTP302
                        /////////////////////////////
                        //
                        //  HTTP 302: Found / Redirect.
                        //
                        if (searchTerm == "https://autodiscover-s.outlook.com/autodiscover/autodiscover.xml")
                        {
                            this.session["ui-backcolor"] = "green";
                            this.session["ui-color"] = "black";
                        }
                        else
                        {
                            // To be determined. Do nothing right now.
                        }
                        //
                        /////////////////////////////
                        #endregion
                        break;
                    case 401:
                        #region HTTP401
                        /////////////////////////////
                        //
                        //  HTTP 401: UNAUTHORIZED.
                        //
                        this.session["ui-backcolor"] = "orange";
                        this.session["ui-color"] = "black";
                        #endregion
                        break;
                    case 403:
                        #region HTTP403
                        /////////////////////////////
                        //
                        //  HTTP 403: FORBIDDEN.
                        //
                        // Simply looking for the term "Access Denied" works fine using utilFindInResponse.
                        // Specific scenario where a web proxy is blocking traffic.
                        if (session.utilFindInResponse("Access Denied", false) > 1)
                        {
                            this.session["ui-backcolor"] = "red";
                            this.session["ui-color"] = "black";
                        }
                        else
                        {
                            // Pick up any 403 Forbidden and write data into the comments box.
                            this.session["ui-backcolor"] = "red";
                            this.session["ui-color"] = "black";
                        }
                        //
                        /////////////////////////////
                        #endregion
                        break;
                    case 404:
                        #region HTTP404
                        /////////////////////////////
                        //
                        //  HTTP 404: Not Found.
                        //
                        this.session["ui-backcolor"] = "orange";
                        this.session["ui-color"] = "black";
                        //
                        /////////////////////////////
                        #endregion
                        break;
                    case 440:
                        #region HTTP440
                        /////////////////////////////
                        //
                        // HTTP 440: Need to know more about these.
                        // For the moment do nothing.
                        //
                        /////////////////////////////
                        #endregion
                        break;
                    case 500:
                        #region HTTP500
                        /////////////////////////////
                        //
                        //  HTTP 500: Internal Server Error.
                        //
                        // Pick up any 500 Internal Server Error and write data into the comments box.
                        // Specific scenario on Outlook and Office 365 invalid DNS lookup.
                        // < Discuss and confirm thinking here, validate with a working trace. Is this a true false positive? Highlight in green? >
                        this.session["ui-backcolor"] = "red";
                        this.session["ui-color"] = "black";
                        #endregion
                        break;
                    case 502:
                        #region HTTP502
                        /////////////////////////////
                        //
                        //  HTTP 502: BAD GATEWAY.
                        //

                        // Specific scenario on Outlook & OFffice 365 Autodiscover false positive on connections to:
                        //      autodiscover.domain.onmicrosoft.com:443
                        if (session.utilFindInResponse("target machine actively refused it", false) > 1)
                        {
                            if (session.utilFindInResponse("autodiscover", false) > 1)
                            {
                                if (session.utilFindInResponse(":443", false) > 1)
                                {
                                    session["ui-backcolor"] = "blue";
                                    session["ui-color"] = "black";
                                }
                            }
                        }
                        // Specific scenario on Outlook and Office 365 invalid DNS lookup.
                        // < Discuss and confirm thinking here, validate with a working trace. Is this a true false positive? Highlight in green? >
                        else if (session.utilFindInResponse("The requested name is valid, but no data of the requested type was found", false) > 1)
                        {
                            if (session.utilFindInResponse(".onmicrosoft.com", false) > 1)
                            {
                                if (session.utilFindInResponse("failed. System.Net.Sockets.SocketException", false) > 1)
                                {
                                    if (session.utilFindInResponse("DNS Lookup for ", false) > 1)
                                    {
                                        session["ui-backcolor"] = "blue";
                                        session["ui-color"] = "black";
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Pick up any other 502 Bad Gateway call it out.
                            session["ui-backcolor"] = "red";
                            session["ui-color"] = "black";
                        }
                        //
                        /////////////////////////////
                        #endregion
                        break;
                    case 503:
                        #region HTTP503
                        /////////////////////////////
                        //
                        //  HTTP 503: SERVICE UNAVAILABLE.
                        //
                        // Call out all 503 Service Unavailable as something to focus on.
                        session["ui-backcolor"] = "red";
                        session["ui-color"] = "black";
                        //
                        /////////////////////////////
                        #endregion
                        break;
                    case 504:
                        #region HTTP504
                        /////////////////////////////
                        //
                        //  HTTP 504: GATEWAY TIMEOUT.
                        //
                        // Call out all 504 Gateway Timeout as something to focus on.
                        session["ui-backcolor"] = "red";
                        session["ui-color"] = "black";
                        //
                        /////////////////////////////
                        #endregion
                        break;
                    default:
                        break;
                }
            }
        }
    }

    #endregion

    public void OnBeforeUnload() { }

    // Make sure the Columns are added to the UI.
    private void EnsureColumn()
    {
        if (bCreatedColumn) return;

        FiddlerApplication.UI.lvSessions.AddBoundColumn("Response Time", 2, 110, "X-iTTLB");
        
        bCreatedColumn = true;
    }

    public void OnPeekAtResponseHeaders(IAutoTamper2 AllSessions) { }
    
    public void AutoTamperRequestBefore(Session oSession) { }

    public void AutoTamperRequestAfter(Session oSession) { }

    public void AutoTamperResponseBefore(Session oSession) { }

    public void AutoTamperResponseAfter(Session oSession) {
        oSession["X-iTTLB"] = oSession.oResponse.iTTLB.ToString();

        /////////////////
        //
        // Call the function to colourise sessions for live traffic capture.
        //
        OnPeekAtResponseHeaders(oSession);
        //
        /////////////////
    }

    public void OnBeforeReturningError(Session oSession) { }

}