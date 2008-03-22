using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.UI;
using System.Web.UI.WebControls;
using N2.Resources;

namespace N2.Web.UI.WebControls
{
	/// <summary>A webcontrol that pulls out the page title and transforms it into a web-compatible name that can be used in url's</summary>
	public class NameEditor : TextBox, IValidator
	{
		private char whitespaceReplacement = '-';
		private bool toLower;
		private bool ascii;
		private string prefix = string.Empty;
		private string suffix = string.Empty;

		/// <summary>Constructs the name editor.</summary>
		public NameEditor()
		{
			MaxLength = 242;
			CssClass = "nameEditor";
		}

		/// <summary>Gets or sets the name of the editor containing the title to input in the name editor.</summary>
		public string TitleEditorName
		{
			get { return (string) (ViewState["TitleEditorName"] ?? "Title"); }
			set { ViewState["TitleEditorName"] = value; }
		}


		public string Prefix
		{
			get { return prefix; }
			set { prefix = value; }
		}

		public string Suffix
		{
			get { return suffix; }
			set { suffix = value; }
		}


		#region Methods

		/// <summary>Initializes the name editor attaching to the title control onchange event.</summary>
		/// <param name="e">ignored</param>
		protected override void OnPreRender(EventArgs e)
		{
			base.OnPreRender(e);

			IItemEditor itemEditor = ItemUtility.FindInParents<IItemEditor>(Parent);

			if (itemEditor != null)
			{
				try
				{
					TextBox tbTitle = itemEditor.AddedEditors[TitleEditorName] as TextBox;
					if (tbTitle != null)
					{
						string s = script + Environment.NewLine +
						           string.Format("$('#{0}').bind('change', function(){{updateName('{0}','{1}', '{2}', {3}, {4});}});", 
												 tbTitle.ClientID,
						                         ClientID,
												 WhitespaceReplacement,
												 ToLower.ToString().ToLower(),
												 Ascii.ToString().ToLower());
						Register.JavaScript(Page, s, ScriptPosition.Header, ScriptOptions.DocumentReady);
					}
				}
				catch (KeyNotFoundException ex)
				{
					throw new N2Exception(
						"No editor definition found for the Title property. The NameEditor copies the title and adjusts it for beeing part of the url. Either add a title editor or use another control to edit the name.",
						ex);
				}
			}
		}

		#region OnPreRender Helpers

		private const string script =
			@"
function updateName(titleid,nameid,whitespace,tolower,ascii){
    var titleBox=document.getElementById(titleid);
	
	var name = titleBox.value.replace(/[%?&/+:<>]|[.]+$/g, '').replace(/[.]+/g, '.').replace(/\s+/g,whitespace);
	if(tolower) name = name.toLowerCase();
	if(ascii) name = name
		.replace(/[��]/g, 'a').replace(/[��]/g, 'A')
		.replace(/�/g, 'ae').replace(/�/g, 'ae')
		.replace(/[��]/g, 'o').replace(/[��]/g, 'O')
		.replace(/[^a-zA-Z0-9_-]/g, '');
    
	var nameBox=document.getElementById(nameid);
	nameBox.value = name;
}
";

		#endregion

		#endregion

		#region IValidator Members

		/// <summary>Gets or sets the error message generated when the name editor contains invalid values.</summary>
		public string ErrorMessage
		{
			get { return (string) ViewState["ErrorMessage"] ?? ""; }
			set { ViewState["ErrorMessage"] = value; }
		}

		/// <summary>Gets or sets wether the name editor's value passes validaton.</summary>
		public bool IsValid
		{
			get { return ViewState["IsValid"] != null ? (bool) ViewState["IsValid"] : true; }
			set { ViewState["IsValid"] = value; }
		}

		public char WhitespaceReplacement
		{
			get { return whitespaceReplacement; }
			set { whitespaceReplacement = value; }
		}

		public bool ToLower
		{
			get { return toLower; }
			set { toLower = value; }
		}

		public bool Ascii
		{
			get { return ascii; }
			set { ascii = value; }
		}

		/// <summary>Validates the name editor's value checking uniqueness and lenght.</summary>
		public void Validate()
		{
			IsValid = true;

			ContentItem currentItem = ItemUtility.FindCurrentItem(Parent);

			if (currentItem != null)
			{
				if (!string.IsNullOrEmpty(Text))
				{
					// Ensure that the chosen name is locally unique
					if (!N2.Context.IntegrityManager.IsLocallyUnique(Text, currentItem))
					{
						//Another item with the same parent and the same name was found 
						ErrorMessage = string.Format("Another item already has the name '{0}'.", Text);
						IsValid = false;
						return;
					}
					// Ensure that the path isn't longer than 260 characters
					if (currentItem.Parent != null &&
					    (currentItem.Parent.Url.Length > 248 || currentItem.Parent.Url.Length + Text.Length > 260))
					{
						ErrorMessage = string.Format("Name too long, the full url cannot exceed 260 characters: {0}", Text);
						IsValid = false;
						return;
					}
					if(Text.IndexOfAny(new char[]{'?', '&', '/', '+', ':', '%'}) > 0)
					{
						ErrorMessage = "Invalid characters in path, % ? & / : + not allowed.";
						IsValid = false;
						return;
					}
				}
			}
		}

		public override void RenderBeginTag(HtmlTextWriter writer)
		{
			writer.Write(Prefix);
			base.RenderBeginTag(writer);
		}

		/// <summary>Renders an additional asterix when validation didn't pass.</summary>
		/// <param name="writer">The writer to render the asterix to.</param>
		public override void RenderEndTag(HtmlTextWriter writer)
		{
			base.RenderEndTag(writer);
			writer.Write(Suffix);
			if (!IsValid)
				writer.Write("<span style='color:red'>*</span>");
		}

		#endregion
	}
}
