using System.IO;
using UnityEngine;

namespace PhobiaReliefTherapy.Data
{
    public static class CloudSetup
    {
        public const string SqlEditorUrl = "https://supabase.com/dashboard/project/vzdtagvmpuhyahewooly/sql/new";

        public static string LoadSql()
        {
            string[] candidates =
            {
                Path.Combine(Directory.GetCurrentDirectory(), "docs", "supabase-admin-schema.sql"),
                Path.Combine(Application.dataPath, "..", "docs", "supabase-admin-schema.sql")
            };

            foreach (var path in candidates)
            {
                string full = Path.GetFullPath(path);
                if (File.Exists(full))
                    return File.ReadAllText(full);
            }

            var resource = Resources.Load<TextAsset>("supabase-admin-schema");
            return resource != null ? resource.text : null;
        }

        public static bool ApplyOnce(out string error)
        {
            error = null;
            string sql = LoadSql();
            if (string.IsNullOrEmpty(sql))
            {
                error = "Could not find docs/supabase-admin-schema.sql";
                return false;
            }

            GUIUtility.systemCopyBuffer = sql;
            Application.OpenURL(SqlEditorUrl);
            return true;
        }
    }
}
