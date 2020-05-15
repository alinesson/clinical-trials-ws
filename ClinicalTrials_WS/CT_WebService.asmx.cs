using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.IO;
using System.Data;
using System.Runtime.Caching;
using Npgsql;
using Newtonsoft.Json;
using System.Web.Script.Services;

namespace CT_WS
{
    /// <summary>
    /// Summary description for CT_WebService
    /// </summary>
    [WebService(Namespace = "http://childrensnational.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]


    public class CT_WebService : System.Web.Services.WebService
    {

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public void GetClinicalTrialData(string str)   
        {
            //Verify str is passed
            if (string.IsNullOrEmpty(str))
            {
                throw new Exception("NCT_ID missing");
            }

            str = str.ToUpper().Trim();
            string JSONString = string.Empty;
            string cacheKey = "GetClinicalTrialData:" + str + ":" + DateTime.Now.ToShortDateString();

            //Check cache
            JSONString = Get<string>(cacheKey);

            if (!string.IsNullOrEmpty(JSONString)) {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);
                return;
            }

            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                string query = "";

                query = "SELECT DISTINCT tbl.nct_id, " + " " +
                                        "tbl.official_title, " + " " +
                                        "tbl.study_type, " + " " +
                                        "tbl.status, " + " " +
                                        "tbl.title, " + " " +
                                        "tbl.state, " + " " +
                                        "tbl.firstreceiveddate, " + " " +
                                        "tbl.lastupdateddate, " + " " +
                                        "tbl.completiondate, " + " " +
                                        "String_agg(DISTINCT key1.NAME, ', ')                AS Keywords1, " + " " +
                                        "CASE " + " " +
                                          "WHEN cal.were_results_reported = 'f' THEN 'false' " + " " +
                                          "WHEN cal.were_results_reported = 't' THEN 'True' " + " " +
                                          "ELSE '' " + " " +
                                        "END                                                 AS Study_Results," + " " +
                                        "eligible.minimum_age " + " " +
                                        "|| ' to ' " + " " +
                                        "|| eligible.maximum_age                             AS Age, " + " " +
                                        "'Need to fix this'                                  AS Group1, " + " " +
                                        "eligible.criteria                                   AS Criteria, " + " " +
                                        "eligible.gender                                     AS Gender, " + " " +
                                        "eligible.healthy_volunteers                         AS AcceptsHealthyVolunteers," + " " +
                                        "String_agg(DISTINCT con.mesh_term, ', ')            AS Conditions,  " + " " +
                                        "String_agg(DISTINCT bi.mesh_term, ', ')             AS Interventions," + " " +
                                        "String_agg(DISTINCT spn.NAME, ', ')                 AS SponsorCollaborator, " + " " +
                                        "String_agg(DISTINCT spn.lead_or_collaborator, ', ') AS SponsorLead, " + " " +
                                        "bs.description                                      AS description1, " + " " +
                                        "des.description										AS detaildescription," + " " +
                                        "tbl.phase, " + " " +
                                        "ds.primary_purpose, " + " " +
                                        "ds.masking, " + " " +
                                        "String_agg(DISTINCT official.role " + " " +
                                                            "|| ':' " + " " +
                                                            "|| official.NAME, ', ')         AS Investigators, " + " " +
                                        "party.responsible_party_type, " + " " +
                                        "String_agg(DISTINCT id.id_value, ', ')              AS StudyID " + " " +
                        "FROM   (SELECT DISTINCT std.nct_id, " + " " +
                                                "std.official_title, " + " " +
                                                "std.study_type                                 AS Study_type, " + " " +
                                                "std.overall_status                             AS Status, " + " " +
                                                "COALESCE(std.brief_title, '') " + " " +
                                                "|| ' ' " + " " +
                                                "|| COALESCE(std.acronym, '')                   AS Title, " + " " +
                                                "String_agg(DISTINCT f.state, ', ')             AS State, " + " " +
                                                //"To_char(std.first_received_date, 'MM/DD/YYYY') AS FirstReceivedDate, " + " " +
                                                //"To_char(std.last_changed_date, 'MM/DD/YYYY')   AS LastUpdatedDate, " + " " +
                                                "Null                                           AS FirstReceivedDate, " + " " +
                                                "Null                                           AS LastUpdatedDate, " + " " +
                                                "To_char(std.completion_date, 'MM/DD/YYYY')     AS CompletionDate, " + " " +
                                                "std.phase " + " " +
                                "FROM   studies std " + " " +
                                       "INNER JOIN facilities f ON std.nct_id = f.nct_id " + " " +
                                       "INNER JOIN sponsors sp ON std.nct_id = sp.nct_id  " + " " +
                                "WHERE  std.nct_id = '" + str + "'  " + " " +
                                       "AND f.country = 'United States' " + " " +
                                       "AND Lower(f.state) IN ( 'district of columbia', 'maryland', 'virginia' ) " + " " +
                                       "AND ( std.completion_date IS NULL OR std.completion_date > CURRENT_DATE ) " + " " +
                                       "AND ( ( Lower(f.NAME) LIKE '%children%' AND Lower(f.NAME) LIKE '%national%' ) " + " " +
                                              "OR (Lower(f.NAME) LIKE '%children%' AND Lower(f.NAME) LIKE '%research%' AND Lower(f.NAME) LIKE '%institute%' ) " + " " +
                                              "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%national%')" + " " +
                                              "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%research%' AND LOWER(sp.name) LIKE '%institute%') ) " + " " +
                                 "GROUP  BY std.nct_id, " + " " +
                                          "std.official_title, " + " " +
                                          "std.study_type, " + " " +
                                          "std.overall_status, " + " " +
                                          "title, " + " " +
                                          "state, " + " " +
                                          //"first_received_date, " + " " +
                                          //"last_changed_date, " + " " +
                                          "completion_date, " + " " +
                                          "std.phase) tbl " + " " +
                               "LEFT JOIN calculated_values cal " + " " +
                                      "ON tbl.nct_id = cal.nct_id " + " " +
                               "LEFT JOIN eligibilities ELIGIBLE " + " " +
                                      "ON tbl.nct_id = ELIGIBLE.nct_id " + " " +
                              "LEFT JOIN browse_conditions CON " + " " +
                                      "ON tbl.nct_id = CON.nct_id " + " " +
                               "LEFT JOIN keywords KEY1 " + " " +
                                      "ON tbl.nct_id = KEY1.nct_id " + " " +
                               "LEFT JOIN sponsors SPN " + " " +
                                      "ON tbl.nct_id = SPN.nct_id " + " " +
                               "LEFT JOIN detailed_descriptions des " + " " +
                                      "ON tbl.nct_id = des.nct_id " + " " +
                               "LEFT JOIN browse_interventions bi " + " " +
                                      "ON tbl.nct_id = bi.nct_id " + " " +
                               "LEFT JOIN designs ds " + " " +
                                      "ON tbl.nct_id = ds.nct_id " + " " +
                               "LEFT JOIN overall_officials official " + " " +
                                      "ON tbl.nct_id = official.nct_id " + " " +
                               "LEFT JOIN responsible_parties party " + " " +
                                      "ON tbl.nct_id = party.nct_id " + " " +
                               "LEFT JOIN id_information id " + " " +
                                      "ON tbl.nct_id = id.nct_id " + " " +
                               "LEFT JOIN brief_summaries bs " + " " +
                                      "ON tbl.nct_id = bs.nct_id " + " " +
                        "GROUP  BY tbl.nct_id, " + " " +
                                  "tbl.official_title, " + " " +
                                  "tbl.study_type, " + " " +
                                  "tbl.status, " + " " +
                                  "tbl.title, " + " " +
                                  "tbl.state, " + " " +
                                  "tbl.firstreceiveddate, " + " " +
                                  "tbl.lastupdateddate, " + " " +
                                  "tbl.completiondate, " + " " +
                                  "study_results, " + " " +
                                  "age, " + " " +
                                  "group1, " + " " +
                                  "criteria, " + " " +
                                  "gender," + " " +
                                  "acceptshealthyvolunteers," + " " +
                                  "bs.description," + " " +
                                  "des.description," + " " +
                                  "tbl.phase, " + " " +
                                  "ds.primary_purpose, " + " " +
                                  "ds.masking," + " " +
                                  "party.responsible_party_type " + " " +
                        "ORDER  BY 1 ";

                string connstring = String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4}",
                                "aact-db.ctti-clinicaltrials.org", "5432", "webteam", "DrBear2018*", "aact");

                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();

                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);

                da.SelectCommand.CommandTimeout = 120 * 60;

                ds.Reset();
                // filling DataSet with result from NpgsqlDataAdapter
                da.Fill(ds);
                // since it C# DataSet can handle multiple tables, we will select first
                dt = ds.Tables[0];
                // connect grid to DataTable

                // since we only showing the result we don't need connection anymore
                conn.Close();
                JSONString = JsonConvert.SerializeObject(dt);

                //Add to cache
                AddItem(JSONString, cacheKey);

                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);
            }
            catch (Exception e)
            {
                throw e;
            }


        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public void GetCentralContacts(string str)
        {
            //Verify str is passed
            if (string.IsNullOrEmpty(str))
            {
                throw new Exception("NCT_ID missing");
            }

            str = str.ToUpper().Trim();
            string JSONString = string.Empty;
            string cacheKey = "GetCentralContacts:" + str + ":" + DateTime.Now.ToShortDateString();

            //Check cache
            JSONString = Get<string>(cacheKey);

            if (!string.IsNullOrEmpty(JSONString))
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);
                return;
            }

            //String xml;
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                string query = "";

                query = "SELECT DISTINCT name, phone, email FROM central_contacts WHERE nct_id='" + str + "'";

                string connstring = String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4}",
                                "aact-db.ctti-clinicaltrials.org", "5432", "webteam", "DrBear2018*", "aact");

                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();
                // data adapter making request from our connection
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);

                ds.Reset();
                // filling DataSet with result from NpgsqlDataAdapter
                da.Fill(ds);
                // since it C# DataSet can handle multiple tables, we will select first
                dt = ds.Tables[0];
                // connect grid to DataTable

                // since we only showing the result we don't need connection anymore
                conn.Close();

                JSONString = JsonConvert.SerializeObject(dt);

                //Add to cache
                AddItem(JSONString, cacheKey);

                Context.Response.Clear();
                Context.Response.ContentType = "application/json";

                Context.Response.Write(JSONString);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public void GetFacilityContacts(string str)
        {
            //Verify str is passed
            if (string.IsNullOrEmpty(str))
            {
                throw new Exception("NCT_ID missing");
            }

            str = str.ToUpper().Trim();
            string JSONString = string.Empty;
            string cacheKey = "GetFacilityContacts:" + str + ":" + DateTime.Now.ToShortDateString();

            //Check cache
            JSONString = Get<string>(cacheKey);

            if (!string.IsNullOrEmpty(JSONString))
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);
                return;
            }

            //String xml;
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                string query = "";

                query = "SELECT DISTINCT name, phone, email FROM facility_contacts WHERE nct_id='" + str + "'";

                string connstring = String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4}",
                                "aact-db.ctti-clinicaltrials.org", "5432", "webteam", "DrBear2018*", "aact");

                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();
                // data adapter making request from our connection
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);

                ds.Reset();
                // filling DataSet with result from NpgsqlDataAdapter
                da.Fill(ds);
                // since it C# DataSet can handle multiple tables, we will select first
                dt = ds.Tables[0];
                // connect grid to DataTable

                // since we only showing the result we don't need connection anymore
                conn.Close();

                JSONString = JsonConvert.SerializeObject(dt);

                //Add to cache
                AddItem(JSONString, cacheKey);

                Context.Response.Clear();
                Context.Response.ContentType = "application/json";

                Context.Response.Write(JSONString);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public void GetInvestigators(string str)
        {
            //Verify str is passed
            if (string.IsNullOrEmpty(str))
            {
                throw new Exception("NCT_ID missing");
            }

            str = str.ToUpper().Trim();
            string JSONString = string.Empty;
            string cacheKey = "GetInvestigators:" + str + ":" + DateTime.Now.ToShortDateString();

            //Check cache
            JSONString = Get<string>(cacheKey);

            if (!string.IsNullOrEmpty(JSONString))
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);
                return;
            }

            //String xml;
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                string query = "";

                query = "SELECT DISTINCT role, name, affiliation FROM Overall_Officials WHERE nct_id='" + str + "' ORDER BY role, name";
                //query = "SELECT DISTINCT role, name, affiliation FROM Overall_Officials WHERE LOWER(role) = LOWER('Principal Investigator') AND nct_id='" + str + "'";

                string connstring = String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4}",
                                "aact-db.ctti-clinicaltrials.org", "5432", "webteam", "DrBear2018*", "aact");

                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();

                // data adapter making request from our connection
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);

                ds.Reset();
                // filling DataSet with result from NpgsqlDataAdapter
                da.Fill(ds);
                // since it C# DataSet can handle multiple tables, we will select first
                dt = ds.Tables[0];

                // since we only showing the result we don't need connection anymore
                conn.Close();
                
                JSONString = JsonConvert.SerializeObject(dt);

                //Add to cache
                AddItem(JSONString, cacheKey);

                Context.Response.Clear();
                Context.Response.ContentType = "application/json";

                Context.Response.Write(JSONString);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public void GetSummaryListByNCTIDs(string str)
        {
            //Verify str is passed
            if (string.IsNullOrEmpty(str)){
                throw new Exception("NCT_IDs missing");
            }

            str = str.ToUpper().Trim();
            string JSONString = string.Empty;
            string cacheKey = "GetSummaryListByNCTIDs:" + str.Replace("\"", string.Empty).Replace("\'", string.Empty).Replace(",", string.Empty) + ":" + DateTime.Now.ToShortDateString();

            //Check cache
            JSONString = Get<string>(cacheKey);

            if (!string.IsNullOrEmpty(JSONString))
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);
                return;
            }

            //String xml;
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                string query = "";

                query = "SELECT DISTINCT   tbl.nct_id, " + " " +
                                          "tbl.official_title,    " + " " +
                                          "tbl.brief_title, " + " " +
                                          "tbl.overall_status, " + " " +
                                          "tbl.phase, " + " " +
                                          "bs.description " + " " +
                        "FROM   ( " + " " +
                                    "SELECT DISTINCT s.nct_id,   " + " " +
                                            "s.official_title,   " + " " +
                                            "s.brief_title, " + " " +
                                            "s.overall_status, " + " " +
                                            "s.phase " + " " +
                                    "FROM	studies s  " + " " +
                                            "INNER JOIN facilities f ON s.nct_id=f.nct_id  " + " " +
                                            "INNER JOIN sponsors sp ON s.nct_id = sp.nct_id  " + " " +
                                    " WHERE 	f.country='United States' " + " " +
                                            "AND LOWER(f.state) in ('district of columbia','maryland', 'virginia') " + " " +
                                            "AND (s.completion_date IS NULL OR s.completion_date > CURRENT_DATE) " + " " +
                                            "AND ((LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%national%') " + " " +
                                                "OR (LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%research%' AND LOWER(f.name) LIKE '%institute%') " + " " +
                                                "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%national%') " + " " +
                                                "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%research%' AND LOWER(sp.name) LIKE '%institute%')) " + " " +
                                            //"AND f.status in ('Enrolling by invitation','Not yet recruiting','Recruiting') " + " " +
                                            "AND s.overall_status in ('Active, not recruiting', 'Approved for marketing', 'Available', 'Enrolling by invitation', 'Recruiting') " + " " +
                                    "GROUP BY s.nct_id, " + " " +
                                               "s.official_title, " + " " +
                                               "s.brief_title,  " + " " +
                                               "s.overall_status, " + " " +
                                               "s.phase " + " " +
                                ") tbl  " + " " +
                        "LEFT JOIN brief_summaries bs ON tbl.nct_id=bs.nct_id  " + " " +
                        "WHERE  (   " + " " +
                                  "UPPER(TRIM(tbl.nct_id)) IN (" + str + ")	 " + " " +
                                ")";

                string connstring = String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4}",
                                "aact-db.ctti-clinicaltrials.org", "5432", "webteam", "DrBear2018*", "aact");

                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();
                // quite complex sql statement
                // string sql = "SELECT  * FROM studies where official_title like '%" + str + "%' limit 10";
                // data adapter making request from our connection

                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);

                da.SelectCommand.CommandTimeout = 120 * 60;

                ds.Reset();
                // filling DataSet with result from NpgsqlDataAdapter
                da.Fill(ds);
                // since it C# DataSet can handle multiple tables, we will select first
                dt = ds.Tables[0];
                // connect grid to DataTable

                // since we only showing the result we don't need connection anymore
                conn.Close();

                JSONString = JsonConvert.SerializeObject(dt);

                //Add to cache
                AddItem(JSONString, cacheKey);

                Context.Response.Clear();
                Context.Response.ContentType = "application/json";

                Context.Response.Write(JSONString);

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public void SearchForNCT(string str)
        {
            //Verify str is passed
            if (string.IsNullOrEmpty(str))
            {
                throw new Exception("NCT_ID missing");
            }

            str = str.ToUpper().Trim();
            string JSONString = string.Empty;
            string cacheKey = "SearchForNCT:" + str + ":" + DateTime.Now.ToShortDateString();

            //Check cache
            JSONString = Get<string>(cacheKey);

            if (!string.IsNullOrEmpty(JSONString))
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);
                return;
            }

            //String xml;
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                string query = "";

                query = "SELECT DISTINCT   tbl.nct_id, " + " " +
                                          "tbl.official_title,    " + " " +
                                          "tbl.brief_title, " + " " +
                                          "tbl.overall_status, " + " " +
                                          "bs.description " + " " +
                        "FROM   ( " + " " +
                                    "SELECT DISTINCT s.nct_id,   " + " " +
                                            "s.official_title,   " + " " +
                                            "s.brief_title, " + " " +
                                            "s.overall_status " + " " +
                                    "FROM	studies s  " + " " +
                                            "INNER JOIN facilities f ON s.nct_id=f.nct_id  " + " " +
                                            "INNER JOIN sponsors sp ON s.nct_id = sp.nct_id  " + " " +
                                    " WHERE 	f.country='United States' " + " " +
                                            "AND LOWER(f.state) in ('district of columbia','maryland', 'virginia') " + " " +
                                            "AND (s.completion_date IS NULL OR s.completion_date > CURRENT_DATE) " + " " +
                                            "AND ((LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%national%') " + " " +
                                                "OR (LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%research%' AND LOWER(f.name) LIKE '%institute%') " + " " +
                                                "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%national%') " + " " +
                                                "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%research%' AND LOWER(sp.name) LIKE '%institute%')) " + " " +
                                            //"AND f.status in ('Enrolling by invitation','Not yet recruiting','Recruiting') " + " " +
                                            "AND s.overall_status in ('Active, not recruiting', 'Approved for marketing', 'Available', 'Enrolling by invitation', 'Recruiting') " + " " +
                                    "GROUP BY s.nct_id, " + " " +
                                               "s.official_title, " + " " +
                                               "s.brief_title,  " + " " +
                                               "s.overall_status " + " " +
                                ") tbl  " + " " +
                        "LEFT JOIN brief_summaries bs ON tbl.nct_id=bs.nct_id  " + " " +
                        "WHERE  (   " + " " +
                                  "LOWER(TRIM(tbl.nct_id)) LIKE LOWER('%" + str + "%')	 " + " " +
                                ")";

                string connstring = String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4}",
                                "aact-db.ctti-clinicaltrials.org", "5432", "webteam", "DrBear2018*", "aact");

                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();
                // quite complex sql statement
                // string sql = "SELECT  * FROM studies where official_title like '%" + str + "%' limit 10";
                // data adapter making request from our connection

                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);

                da.SelectCommand.CommandTimeout = 120 * 60;

                ds.Reset();
                // filling DataSet with result from NpgsqlDataAdapter
                da.Fill(ds);
                // since it C# DataSet can handle multiple tables, we will select first
                dt = ds.Tables[0];
                // connect grid to DataTable

                // since we only showing the result we don't need connection anymore
                conn.Close();

                JSONString = JsonConvert.SerializeObject(dt);

                //Add to cache
                AddItem(JSONString, cacheKey);

                Context.Response.Clear();
                Context.Response.ContentType = "application/json";

                Context.Response.Write(JSONString);

            }
            catch (Exception e)
            {
                throw e;
            }
        }


        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public void SearchForInvestigator(string str)
        {
            //Verify str is passed
            if (string.IsNullOrEmpty(str))
            {
                throw new Exception("Investigator Name missing");
            }

            str = str.ToUpper().Trim();
            string JSONString = string.Empty;
            string cacheKey = "SearchForInvestigator:" + str + ":" + DateTime.Now.ToShortDateString();

            //Check cache
            JSONString = Get<string>(cacheKey);

            if (!string.IsNullOrEmpty(JSONString))
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);
                return;
            }

            //String xml;
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                string query = "";
                query = "SELECT DISTINCT   tbl.nct_id, " + " " +
                                          "tbl.official_title,    " + " " +
                                          "tbl.brief_title, " + " " +
                                          "tbl.overall_status, " + " " +
                                          "bs.description " + " " +
                        "FROM   ( " + " " +
                                    "SELECT DISTINCT s.nct_id,   " + " " +
                                            "s.official_title,   " + " " +
                                            "s.brief_title, " + " " +
                                            "s.overall_status " + " " +
                                    "FROM	studies s  " + " " +
                                            "INNER JOIN facilities f ON s.nct_id=f.nct_id  " + " " +
                                            "INNER JOIN sponsors sp ON s.nct_id = sp.nct_id  " + " " +
                                   " WHERE 	f.country='United States' " + " " +
                                            "AND LOWER(f.state) in ('district of columbia','maryland', 'virginia') " + " " +
                                            "AND (s.completion_date IS NULL OR s.completion_date > CURRENT_DATE) " + " " +
                                            "AND ((LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%national%') " + " " +
                                                "OR (LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%research%' AND LOWER(f.name) LIKE '%institute%') " + " " +
                                                "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%national%') " + " " +
                                                "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%research%' AND LOWER(sp.name) LIKE '%institute%')) " + " " +
                                            //"AND f.status in ('Enrolling by invitation','Not yet recruiting','Recruiting') " + " " +
                                            "AND s.overall_status in ('Active, not recruiting', 'Approved for marketing', 'Available', 'Enrolling by invitation', 'Recruiting') " + " " +
                                    "GROUP BY s.nct_id, " + " " +
                                                "s.official_title, " + " " +
                                                "s.brief_title,  " + " " +
                                                "s.overall_status " + " " +
                                ") tbl  " + " " +
                        "LEFT JOIN brief_summaries bs ON tbl.nct_id=bs.nct_id  " + " " +
                        "LEFT JOIN overall_officials oo ON tbl.nct_id=oo.nct_id  " + " " +
                        "LEFT JOIN facility_investigators fi ON tbl.nct_id=fi.nct_id  " + " " +
                        "WHERE  (   " + " " +
                                "(LOWER(TRIM(oo.name)) LIKE LOWER('%" + str + "%') AND LOWER(oo.role) = LOWER('Principal Investigator'))  " + " " +
                                "OR (LOWER(TRIM(fi.name)) LIKE LOWER('%" + str + "%') AND LOWER(fi.role) in (LOWER('Study Chair'), LOWER('Study Director')))  " + " " +
                                ")";

                string connstring = String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4}",
                                "aact-db.ctti-clinicaltrials.org", "5432", "webteam", "DrBear2018*", "aact");

                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();
                // quite complex sql statement
                // string sql = "SELECT  * FROM studies where official_title like '%" + str + "%' limit 10";
                // data adapter making request from our connection

                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);

                da.SelectCommand.CommandTimeout = 120 * 60;

                ds.Reset();
                // filling DataSet with result from NpgsqlDataAdapter
                da.Fill(ds);
                // since it C# DataSet can handle multiple tables, we will select first
                dt = ds.Tables[0];
                // connect grid to DataTable

                // since we only showing the result we don't need connection anymore
                conn.Close();

                JSONString = JsonConvert.SerializeObject(dt);

                //Add to cache
                AddItem(JSONString, cacheKey);

                Context.Response.Clear();
                Context.Response.ContentType = "application/json";

                Context.Response.Write(JSONString);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public void SearchForContact(string str)
        {
            //Verify str is passed
            if (string.IsNullOrEmpty(str))
            {
                throw new Exception("Contact Name missing");
            }

            str = str.ToUpper().Trim();
            string JSONString = string.Empty;
            string cacheKey = "SearchForContact:" + str + ":" + DateTime.Now.ToShortDateString();

            //Check cache
            JSONString = Get<string>(cacheKey);

            if (!string.IsNullOrEmpty(JSONString))
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);
                return;
            }

            //String xml;
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                string query = "";
                query = "SELECT DISTINCT   tbl.nct_id, " + " " +
                                          "tbl.official_title,    " + " " +
                                          "tbl.brief_title, " + " " +
                                          "tbl.overall_status, " + " " +
                                          "bs.description " + " " +
                        "FROM   ( " + " " +
                                    "SELECT DISTINCT s.nct_id,   " + " " +
                                            "s.official_title,   " + " " +
                                            "s.brief_title, " + " " +
                                            "s.overall_status, " + " " +
                                            "f.id as facility_id" + " " +
                                    "FROM	studies s  " + " " +
                                            "INNER JOIN facilities f ON s.nct_id=f.nct_id  " + " " +
                                            "INNER JOIN sponsors sp ON s.nct_id = sp.nct_id  " + " " +
                                   " WHERE 	f.country='United States' " + " " +
                                            "AND LOWER(f.state) in ('district of columbia','maryland', 'virginia') " + " " +
                                            "AND (s.completion_date IS NULL OR s.completion_date > CURRENT_DATE) " + " " +
                                            "AND ((LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%national%') " + " " +
                                                "OR (LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%research%' AND LOWER(f.name) LIKE '%institute%') " + " " +
                                                "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%national%') " + " " +
                                                "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%research%' AND LOWER(sp.name) LIKE '%institute%')) " + " " +

                                            //"AND f.status in ('Enrolling by invitation','Not yet recruiting','Recruiting') " + " " +
                                            "AND s.overall_status in ('Active, not recruiting', 'Approved for marketing', 'Available', 'Enrolling by invitation', 'Recruiting') " + " " +
                                    "GROUP BY s.nct_id, " + " " +
                                               "s.official_title, " + " " +
                                               "s.brief_title,  " + " " +
                                               "s.overall_status, " + " " +
                                               "f.id  " + " " +
                                ") tbl  " + " " +
                        "LEFT JOIN brief_summaries bs ON tbl.nct_id=bs.nct_id  " + " " +
                        "LEFT JOIN central_contacts cc ON tbl.nct_id=cc.nct_id  " + " " +
                        "LEFT JOIN facility_contacts fc ON tbl.facility_id=fc.facility_id  " + " " +
                        "WHERE  (   " + " " +
                                "LOWER(TRIM(cc.name)) LIKE LOWER('%" + str + "%')  " + " " +
                                "OR LOWER(TRIM(fc.name)) LIKE LOWER('%" + str + "%')  " + " " +
                                ")";

                string connstring = String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4}",
                                "aact-db.ctti-clinicaltrials.org", "5432", "webteam", "DrBear2018*", "aact");

                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();
                // quite complex sql statement
                // string sql = "SELECT  * FROM studies where official_title like '%" + str + "%' limit 10";
                // data adapter making request from our connection

                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);

                da.SelectCommand.CommandTimeout = 120 * 60;

                ds.Reset();
                // filling DataSet with result from NpgsqlDataAdapter
                da.Fill(ds);
                // since it C# DataSet can handle multiple tables, we will select first
                dt = ds.Tables[0];
                // connect grid to DataTable

                // since we only showing the result we don't need connection anymore
                conn.Close();

                JSONString = JsonConvert.SerializeObject(dt);

                //Add to cache
                AddItem(JSONString, cacheKey);

                Context.Response.Clear();
                Context.Response.ContentType = "application/json";

                Context.Response.Write(JSONString);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public void SearchForCondTreatKey(string str)
        {
            //Verify str is passed
            if (string.IsNullOrEmpty(str))
            {
                throw new Exception("Condition, Treatment or Keyword missing");
            }

            str = str.ToUpper().Trim();
            string JSONString = string.Empty;
            string cacheKey = "SearchForCondTreatKey:" + str + ":" + DateTime.Now.ToShortDateString();

            //Check cache
            JSONString = Get<string>(cacheKey);

            if (!string.IsNullOrEmpty(JSONString))
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);
                return;
            }

            //String xml;
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                string query = "";

                query = "SELECT DISTINCT   tbl.nct_id, " + " " +
                                          "tbl.official_title,    " + " " +
                                          "tbl.brief_title, " + " " +
                                          "tbl.overall_status, " + " " +
                                          "bs.description " + " " +
                        "FROM   ( " + " " +
                                    "SELECT DISTINCT s.nct_id,   " + " " +
                                            "s.official_title,   " + " " +
                                            "s.brief_title, " + " " +
                                            "s.overall_status " + " " +
                                    "FROM	studies s  " + " " +
                                            "INNER JOIN facilities f ON s.nct_id=f.nct_id  " + " " +
                                            "INNER JOIN sponsors sp ON s.nct_id = sp.nct_id  " + " " +
                                   " WHERE 	f.country='United States' " + " " +
                                            "AND LOWER(f.state) in ('district of columbia','maryland', 'virginia') " + " " +
                                            "AND (s.completion_date IS NULL OR s.completion_date > CURRENT_DATE) " + " " +
                                            "AND ((LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%national%') " + " " +
                                                "OR (LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%research%' AND LOWER(f.name) LIKE '%institute%') " + " " +
                                                "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%national%') " + " " +
                                                "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%research%' AND LOWER(sp.name) LIKE '%institute%')) " + " " +
                                            //"AND f.status in ('Enrolling by invitation','Not yet recruiting','Recruiting') " + " " +
                                            "AND s.overall_status in ('Active, not recruiting', 'Approved for marketing', 'Available', 'Enrolling by invitation', 'Recruiting') " + " " +
                                    "GROUP BY s.nct_id, " + " " +
                                                "s.official_title, " + " " +
                                                "s.brief_title,  " + " " +
                                                "s.overall_status " + " " +
                                ") tbl  " + " " +
                        "LEFT JOIN brief_summaries bs ON tbl.nct_id=bs.nct_id  " + " " +
                        "LEFT JOIN browse_conditions bc ON tbl.nct_id=bc.nct_id " + " " +
                        "LEFT JOIN browse_interventions bi ON tbl.nct_id=bi.nct_id  " + " " +
                        "LEFT JOIN keywords k ON tbl.nct_id=k.nct_id  " + " " +
                        "WHERE  (   " + " " +
                           "LOWER(TRIM(tbl.brief_title)) LIKE LOWER('%" + str + "%')" + " " +
                               "OR LOWER(TRIM(tbl.official_title)) LIKE LOWER('%" + str + "%')" + " " +
                               "OR LOWER(TRIM(bc.MESH_TERM)) LIKE LOWER('%" + str + "%')		" + " " +
                               "OR LOWER(TRIM(bi.MESH_TERM)) LIKE LOWER('%" + str + "%')	" + " " +
                               "OR LOWER(TRIM(k.name)) LIKE LOWER('%" + str + "%')	" + " " +
                        ")";


                string connstring = String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4}",
                                "aact-db.ctti-clinicaltrials.org", "5432", "webteam", "DrBear2018*", "aact");

                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();

                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);

                da.SelectCommand.CommandTimeout = 120 * 60;

                ds.Reset();
                // filling DataSet with result from NpgsqlDataAdapter
                da.Fill(ds);
                // since it C# DataSet can handle multiple tables, we will select first
                dt = ds.Tables[0];
                // connect grid to DataTable

                // since we only showing the result we don't need connection anymore
                conn.Close();

                JSONString = JsonConvert.SerializeObject(dt);

                //Add to cache
                AddItem(JSONString, cacheKey);

                Context.Response.Clear();
                Context.Response.ContentType = "application/json";

                Context.Response.Write(JSONString);

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public void AutocompleteNCT()
        {
            string JSONString = string.Empty;
            string cacheKey = "AutocompleteNCT:" + DateTime.Now.ToShortDateString();

            //Check cache
            JSONString = Get<string>(cacheKey);

            if (!string.IsNullOrEmpty(JSONString))
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);
                return;
            }

            //String xml;
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                string query = "";

                query = "SELECT DISTINCT s.nct_id as Value  " + " " +
                                    "FROM	studies s  " + " " +
                                            "INNER JOIN facilities f ON s.nct_id=f.nct_id  " + " " +
                                            "INNER JOIN sponsors sp ON s.nct_id = sp.nct_id  " + " " +
                                   " WHERE 	f.country='United States' " + " " +
                                            "AND LOWER(f.state) in ('district of columbia','maryland', 'virginia') " + " " +
                                            "AND (s.completion_date IS NULL OR s.completion_date > CURRENT_DATE) " + " " +
                                            "AND ((LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%national%') " + " " +
                                                "OR (LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%research%' AND LOWER(f.name) LIKE '%institute%') " + " " +
                                                "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%national%') " + " " +
                                                "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%research%' AND LOWER(sp.name) LIKE '%institute%')) " + " " +
                                            //"AND f.status in ('Enrolling by invitation','Not yet recruiting','Recruiting') " + " " +
                                            "AND s.overall_status in ('Active, not recruiting', 'Approved for marketing', 'Available', 'Enrolling by invitation', 'Recruiting') " + " ";


                string connstring = String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4}",
                                "aact-db.ctti-clinicaltrials.org", "5432", "webteam", "DrBear2018*", "aact");

                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();

                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);

                da.SelectCommand.CommandTimeout = 120 * 60;

                ds.Reset();
                // filling DataSet with result from NpgsqlDataAdapter
                da.Fill(ds);
                // since it C# DataSet can handle multiple tables, we will select first
                dt = ds.Tables[0];
                // connect grid to DataTable

                // since we only showing the result we don't need connection anymore
                conn.Close();

                List<string> lstValue = new List<string>();
                foreach (DataRow dr in dt.Rows) {
                    lstValue.Add(dr["value"].ToString());                        
                }

                JSONString = JsonConvert.SerializeObject(lstValue);

                //Add to cache
                AddItem(JSONString, cacheKey);

                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public void AutocompleteCondTreatKey()
        {

            string JSONString = string.Empty;
            string cacheKey = "AutocompleteCondTreatKey:" + DateTime.Now.ToShortDateString();


            //Check cache
            JSONString = Get<string>(cacheKey);

            if (!string.IsNullOrEmpty(JSONString))
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);
                return;
            }

            //String xml;
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                string query = "";

                query = "SELECT DISTINCT value " +
                        "FROM   (" +
                            "SELECT DISTINCT UPPER(bc.MESH_TERM) as value    " + " " +
                            "FROM	studies s   " + " " +
                            "        INNER JOIN facilities f ON s.nct_id=f.nct_id   " + " " +
                            "        INNER JOIN browse_conditions bc ON s.nct_id=bc.nct_id   " + " " +
                            "        INNER JOIN sponsors sp ON s.nct_id = sp.nct_id  " + " " +

                            "WHERE 	f.country='United States' " + " " +
                                    "AND LOWER(f.state) in ('district of columbia','maryland', 'virginia') " + " " +
                                    "AND (s.completion_date IS NULL OR s.completion_date > CURRENT_DATE) " + " " +
                                    "AND ((LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%national%') " + " " +
                                        "OR (LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%research%' AND LOWER(f.name) LIKE '%institute%') " + " " +
                                        "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%national%') " + " " +
                                        "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%research%' AND LOWER(sp.name) LIKE '%institute%')) " + " " +

                                    //"AND f.status in ('Enrolling by invitation','Not yet recruiting','Recruiting') " + " " +
                                    "AND s.overall_status in ('Active, not recruiting', 'Approved for marketing', 'Available', 'Enrolling by invitation', 'Recruiting') " + " " +
                            "UNION ALL   " + " " +
                            "SELECT DISTINCT UPPER(bi.MESH_TERM) as value    " + " " +
                            "FROM	studies s   " + " " +
                            "        INNER JOIN facilities f ON s.nct_id=f.nct_id   " + " " +
                            "        INNER JOIN browse_interventions bi ON s.nct_id=bi.nct_id   " + " " +
                            "        INNER JOIN sponsors sp ON s.nct_id = sp.nct_id  " + " " +
                            "WHERE 	f.country='United States' " + " " +
                                    "AND LOWER(f.state) in ('district of columbia','maryland', 'virginia') " + " " +
                                    "AND (s.completion_date IS NULL OR s.completion_date > CURRENT_DATE) " + " " +
                                    "AND ((LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%national%') " + " " +
                                        "OR (LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%research%' AND LOWER(f.name) LIKE '%institute%') " + " " +
                                        "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%national%') " + " " +
                                        "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%research%' AND LOWER(sp.name) LIKE '%institute%')) " + " " +
                                    //"AND f.status in ('Enrolling by invitation','Not yet recruiting','Recruiting') " + " " +
                                    "AND s.overall_status in ('Active, not recruiting', 'Approved for marketing', 'Available', 'Enrolling by invitation', 'Recruiting') " + " " +
                            "UNION ALL   " + " " +
                            "SELECT DISTINCT UPPER(k.name) as value    " + " " +
                            "FROM	studies s   " + " " +
                            "        INNER JOIN facilities f ON s.nct_id=f.nct_id   " + " " +
                            "        INNER JOIN keywords k ON s.nct_id=k.nct_id    " + " " +
                            "        INNER JOIN sponsors sp ON s.nct_id = sp.nct_id  " + " " +
                            "WHERE 	f.country='United States' " + " " +
                                    "AND LOWER(f.state) in ('district of columbia','maryland', 'virginia') " + " " +
                                    "AND (s.completion_date IS NULL OR s.completion_date > CURRENT_DATE) " + " " +
                                    "AND ((LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%national%') " + " " +
                                        "OR (LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%research%' AND LOWER(f.name) LIKE '%institute%') " + " " +
                                        "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%national%') " + " " +
                                        "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%research%' AND LOWER(sp.name) LIKE '%institute%')) " + " " +
                                    //"AND f.status in ('Enrolling by invitation','Not yet recruiting','Recruiting') " + " " +
                                    "AND s.overall_status in ('Active, not recruiting', 'Approved for marketing', 'Available', 'Enrolling by invitation', 'Recruiting') " +
                        ") as tbl " +
                        "ORDER BY value"; 


                string connstring = String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4}",
                                "aact-db.ctti-clinicaltrials.org", "5432", "webteam", "DrBear2018*", "aact");

                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();

                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);

                da.SelectCommand.CommandTimeout = 120 * 60;

                ds.Reset();
                // filling DataSet with result from NpgsqlDataAdapter
                da.Fill(ds);
                // since it C# DataSet can handle multiple tables, we will select first
                dt = ds.Tables[0];
                // connect grid to DataTable

                // since we only showing the result we don't need connection anymore
                conn.Close();

                List<string> lstValue = new List<string>();
                foreach (DataRow dr in dt.Rows)
                {
                    lstValue.Add(dr["value"].ToString());
                }

                JSONString = JsonConvert.SerializeObject(lstValue);

                //Add to cache
                AddItem(JSONString, cacheKey);

                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public void AutocompleteContact()
        {
            string JSONString = string.Empty;
            string cacheKey = "AutocompleteContact:" + DateTime.Now.ToShortDateString();

            //Check cache
            JSONString = Get<string>(cacheKey);

            if (!string.IsNullOrEmpty(JSONString))
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);
                return;
            }

            //String xml;
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                string query = "";

                query = "SELECT DISTINCT value " +
                        "FROM   (" + 
                            "SELECT DISTINCT cc.name as value     " + " " +
                            "FROM	studies s    " + " " +
                            "        INNER JOIN facilities f ON s.nct_id=f.nct_id    " + " " +
                            "        INNER JOIN central_contacts cc ON s.nct_id=cc.nct_id " + " " +
                            "        INNER JOIN sponsors sp ON s.nct_id = sp.nct_id  " + " " +
                            "WHERE 	f.country='United States' " + " " +
                                    "AND LOWER(f.state) in ('district of columbia','maryland', 'virginia') " + " " +
                                    "AND (s.completion_date IS NULL OR s.completion_date > CURRENT_DATE) " + " " +
                                    "AND ((LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%national%') " + " " +
                                        "OR (LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%research%' AND LOWER(f.name) LIKE '%institute%') " + " " +
                                        "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%national%') " + " " +
                                        "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%research%' AND LOWER(sp.name) LIKE '%institute%')) " + " " +
                                    //"AND f.status in ('Enrolling by invitation','Not yet recruiting','Recruiting') " + " " +
                                    "AND s.overall_status in ('Active, not recruiting', 'Approved for marketing', 'Available', 'Enrolling by invitation', 'Recruiting') " + " " +
                            "        AND NOT cc.name = ''   " + " " +
                            "UNION ALL    " + " " +
                            "SELECT DISTINCT fc.name as value     " + " " +
                            "FROM	studies s    " + " " +
                            "        INNER JOIN facilities f ON s.nct_id=f.nct_id    " + " " +
                            "        INNER JOIN facility_contacts fc ON f.id=fc.facility_id 	   " + " " +
                            "        INNER JOIN sponsors sp ON s.nct_id = sp.nct_id  " + " " +
                            "WHERE 	f.country='United States' " + " " +
                                    "AND LOWER(f.state) in ('district of columbia','maryland', 'virginia') " + " " +
                                    "AND (s.completion_date IS NULL OR s.completion_date > CURRENT_DATE) " + " " +
                                    "AND ((LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%national%') " + " " +
                                        "OR (LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%research%' AND LOWER(f.name) LIKE '%institute%') " + " " +
                                        "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%national%') " + " " +
                                        "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%research%' AND LOWER(sp.name) LIKE '%institute%')) " + " " +

                                    //"AND f.status in ('Enrolling by invitation','Not yet recruiting','Recruiting') " + " " +
                                    "AND s.overall_status in ('Active, not recruiting', 'Approved for marketing', 'Available', 'Enrolling by invitation', 'Recruiting') " + " " +
                            "        AND NOT fc.name = ''" +
                        ") as tbl " +
                        "ORDER BY value"; ;

                string connstring = String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4}",
                                "aact-db.ctti-clinicaltrials.org", "5432", "webteam", "DrBear2018*", "aact");

                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();

                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);

                da.SelectCommand.CommandTimeout = 120 * 60;

                ds.Reset();
                // filling DataSet with result from NpgsqlDataAdapter
                da.Fill(ds);
                // since it C# DataSet can handle multiple tables, we will select first
                dt = ds.Tables[0];
                // connect grid to DataTable

                // since we only showing the result we don't need connection anymore
                conn.Close();

                List<string> lstValue = new List<string>();
                foreach (DataRow dr in dt.Rows)
                {
                    lstValue.Add(dr["value"].ToString());
                }

                JSONString = JsonConvert.SerializeObject(lstValue);

                //Add to cache
                AddItem(JSONString, cacheKey);

                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public void AutocompleteInvestigator()
        {
            string JSONString = string.Empty;
            string cacheKey = "AutocompleteInvestigator:" + DateTime.Now.ToShortDateString();

            //Check cache
            JSONString = Get<string>(cacheKey);

            if (!string.IsNullOrEmpty(JSONString))
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);
                return;
            }

            //String xml;
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                string query = "";

                query = "SELECT DISTINCT value " +
                        "FROM   (" +
                            "SELECT DISTINCT oo.name as value " +
                            "FROM	studies s       " +
                            "        INNER JOIN facilities f ON s.nct_id=f.nct_id       " +
                            "        INNER JOIN overall_officials oo ON s.nct_id=oo.nct_id   " +
                            "        INNER JOIN sponsors sp ON s.nct_id = sp.nct_id  " + " " +
                            "WHERE 	f.country='United States'   " +
                                    "AND LOWER(f.state) in ('district of columbia','maryland', 'virginia') " + " " +
                                    "AND (s.completion_date IS NULL OR s.completion_date > CURRENT_DATE) " + " " +
                                    "AND ((LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%national%') " + " " +
                                        "OR (LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%research%' AND LOWER(f.name) LIKE '%institute%') " + " " +
                                        "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%national%') " + " " +
                                        "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%research%' AND LOWER(sp.name) LIKE '%institute%')) " + " " +
                                    //"AND f.status in ('Enrolling by invitation','Not yet recruiting','Recruiting') " + " " +
                                    "AND s.overall_status in ('Active, not recruiting', 'Approved for marketing', 'Available', 'Enrolling by invitation', 'Recruiting') " + " " +
                            "        AND NOT oo.name = ''      " +
                            "UNION ALL  " +
                            "SELECT DISTINCT fi.name as value        " +
                            "FROM	studies s       " +
                            "        INNER JOIN facilities f ON s.nct_id=f.nct_id       " +
                            "        INNER JOIN facility_investigators fi ON s.nct_id=fi.nct_id       " +
                            "        INNER JOIN sponsors sp ON s.nct_id = sp.nct_id  " + " " +
                            "WHERE 	f.country='United States' " + " " +
                                    "AND LOWER(f.state) in ('district of columbia','maryland', 'virginia') " + " " +
                                    "AND (s.completion_date IS NULL OR s.completion_date > CURRENT_DATE) " + " " +
                                    "AND ((LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%national%') " + " " +
                                        "OR (LOWER(f.name) LIKE '%children%' AND LOWER(f.name) LIKE '%research%' AND LOWER(f.name) LIKE '%institute%') " + " " +
                                        "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%national%') " + " " +
                                        "OR (LOWER(sp.name) LIKE '%children%' AND LOWER(sp.name) LIKE '%research%' AND LOWER(sp.name) LIKE '%institute%')) " + " " +
                                    //"AND f.status in ('Enrolling by invitation','Not yet recruiting','Recruiting') " + " " +
                                    "AND s.overall_status in ('Active, not recruiting', 'Approved for marketing', 'Available', 'Enrolling by invitation', 'Recruiting') " + " " +
                            "        AND NOT fi.name = ''" +
                        ") as tbl " +
                        "ORDER BY value";

                string connstring = String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4}",
                                "aact-db.ctti-clinicaltrials.org", "5432", "webteam", "DrBear2018*", "aact");

                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();

                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);

                da.SelectCommand.CommandTimeout = 120 * 60;

                ds.Reset();
                // filling DataSet with result from NpgsqlDataAdapter
                da.Fill(ds);
                // since it C# DataSet can handle multiple tables, we will select first
                dt = ds.Tables[0];
                // connect grid to DataTable

                // since we only showing the result we don't need connection anymore
                conn.Close();

                List<string> lstValue = new List<string>();
                foreach (DataRow dr in dt.Rows)
                {
                    lstValue.Add(dr["value"].ToString());
                }

                JSONString = JsonConvert.SerializeObject(lstValue);

                //Add to cache
                AddItem(JSONString, cacheKey);

                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(JSONString);

            }
            catch (Exception e)
            {
                throw e;
            }
        }
        #region Methods

        private void AddItem(object objectToCache, string key) {
            ObjectCache cache = MemoryCache.Default;
            cache.Add(key, objectToCache, DateTime.Now.AddDays(1));
        }

        static readonly ObjectCache Cache = MemoryCache.Default;
        private static T Get<T>(string key) where T : class {
            try
            {
                return (T)Cache[key];
            }
            catch {
                return null;
            }
        }
        #endregion
    }
}
