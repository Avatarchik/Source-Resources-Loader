using UnityEngine;
using System.Collections.Generic;

public class SourceEntityInfo : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.DrawCube(transform.position, Vector3.one / 8f);
    }

    public void Configure(List<string> data)
    {
        string Classname = data[data.FindIndex(n => n == "classname") + 1];
        string Targetname = data[data.FindIndex(n => n == "targetname") + 1];

        name = string.Concat(Targetname, " (", Classname, ")");

        if (data.Contains("origin"))
        {
            string[] Array = data[data.FindIndex(n => n == "origin") + 1].Split(new char[] { ' ' });

            while (Array.Length != 3)
            {
                int TempIndex = data.FindIndex(n => n == "origin") + 1;
                Array = data[data.FindIndex(TempIndex, n => n == "origin") + 1].Split(new char[] { ' ' });
            }

            transform.position = new Vector3(-float.Parse(Array[0]), float.Parse(Array[2]), -float.Parse(Array[1])) * Configuration.WorldScale;
        }

        if (data.Contains("angles"))
        {
            string[] Array = data[data.FindIndex(n => n == "angles") + 1].Split(new char[] { ' ' });
            Vector3 EulerAngles = new Vector3(-float.Parse(Array[2]), -float.Parse(Array[1]), -float.Parse(Array[0]));

            if (data.Contains("pitch"))
                EulerAngles.x = -float.Parse(data[data.FindIndex(n => n == "pitch") + 1]);

            transform.eulerAngles = EulerAngles;
        }

        if (Classname.Equals("light_environment"))
        {
            string[] Array = data[data.FindIndex(n => n == "_ambient") + 1].Split(new char[] { ' ' });
            RenderSettings.ambientLight = new Color32(byte.Parse(Array[0]), byte.Parse(Array[1]), byte.Parse(Array[2]), 255);
        }

		if (Classname.Contains("func_") && !Classname.Equals("func_illusionary"))
		{
			for (int i = 0; i < gameObject.transform.childCount; i++)
			{
				Transform Child = gameObject.transform.GetChild(i);
				Child.gameObject.AddComponent<MeshCollider>();
            }
        }

		if (data.Contains("rendercolor"))
		{
			string[] Array = data[data.FindIndex(n => n == "rendercolor") + 1].Split(new char[] { ' ' });
			for (int i = 0; i < gameObject.transform.childCount; i++)
			{
				Color32 RenderColor = new Color32(byte.Parse(Array[0]), byte.Parse(Array[1]), byte.Parse(Array[2]), 255);

				Transform Child = gameObject.transform.GetChild(i);
				Child.gameObject.GetComponent<MeshRenderer>().sharedMaterial.color = RenderColor;
            }
        }
        
        if (data.Contains("rendermode"))
		{
			if (data[data.FindIndex(n => n == "rendermode") + 1] == "10")
			{
				for (int i = 0; i < gameObject.transform.childCount; i++)
				{
					Transform Child = gameObject.transform.GetChild(i);
					Child.gameObject.GetComponent<MeshRenderer>().enabled = false;
                }
            }
        }
    }
}
