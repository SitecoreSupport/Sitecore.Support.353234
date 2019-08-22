namespace Sitecore.Support.Shell.Applications.ContentManager
{
    using Sitecore;
    using Sitecore.Collections;
    using Sitecore.Configuration;
    using Sitecore.Data.Validators;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Shell.Applications.ContentManager;
    using Sitecore.Text;
    using Sitecore.Web;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Sheer;
    using System;
    using System.Web.UI;

    public class FieldEditorForm : Sitecore.Shell.Applications.ContentManager.FieldEditorForm
    {
        protected override void OnPreRendered(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            this.UpdateEditor();
            Context.ClientPage.Modified = false;
        }

        private void UpdateEditor()
        {
            if (!Context.ClientPage.IsEvent || this.SectionToggling)
            {
                if (this.SectionToggling)
                {
                    base.FieldInfo.Clear();
                }
                Border control = new Border();
                this.ContentEditor.Controls.Clear();
                control.ID = "Editors";
                Context.ClientPage.AddControl(this.ContentEditor, control);
                this.RenderEditor(control);
                this.UpdateValidatorBar(control);
            }
        }

        private void UpdateValidatorBar(Border parent)
        {
            Assert.ArgumentNotNull(parent, "parent");
            if (Sitecore.Shell.UserOptions.ContentEditor.ShowValidatorBar)
            {
                Sitecore.Data.Validators.ValidatorCollection validators = this.BuildValidators(ValidatorsMode.ValidatorBar);
                ValidatorManager.Validate(validators, new ValidatorOptions(false));
                string text = ValidatorBarFormatter.RenderValidationResult(validators);
                bool flag = text.IndexOf("Applications/16x16/bullet_square_grey.png", StringComparison.InvariantCulture) >= 0;
                System.Web.UI.Control control = parent.FindControl("ValidatorPanel");
                if (control != null)
                {
                    control.Controls.Add(new LiteralControl(text));
                    Context.ClientPage.FindControl("ContentEditorForm").Controls.Add(new LiteralControl($"<input type=\"hidden\" id=\"scHasValidators\" name=\"scHasValidators\" value=\"{ ((validators.Count > 0) ? "1" : string.Empty)}\"/>"));
                    if (flag)
                    {
                        control.Controls.Add(new LiteralControl($"<script type=\"text / javascript\" language=\"javascript\">window.setTimeout('scContent.updateValidators()', {Settings.Validators.UpdateFrequency})</script>"));
                    }
                    control.Controls.Add(new LiteralControl("<script type=\"text/javascript\" language=\"javascript\">scContent.updateFieldMarkers()</script>"));
                }
            }
        }

        private void RenderEditor(Border parent)
        {
            Assert.ArgumentNotNull(parent, "parent");
            Assert.IsNotNull(this.Options, "Editor options");
            FieldEditor editor1 = new FieldEditor();
            editor1.DefaultIcon = this.Options.Icon;
            editor1.DefaultTitle = this.Options.Title;
            editor1.PreserveSections = this.Options.PreserveSections;
            editor1.ShowInputBoxes = this.Options.ShowInputBoxes;
            editor1.ShowSections = this.Options.ShowSections;
            if (!Context.ClientPage.IsEvent)
            {
                if (!string.IsNullOrEmpty(this.Options.Title))
                {
                    this.DialogTitle.Text = this.Options.Title;
                }
                if (!string.IsNullOrEmpty(this.Options.Text))
                {
                    this.DialogText.Text = this.Options.Text;
                }
                if (!string.IsNullOrEmpty(this.Options.Icon))
                {
                    this.DialogIcon.Src = this.Options.Icon;
                }
            }

            #region Added decoding of encoded html characters

            foreach (var field in Options.Fields)
            {
                field.Value = field.Value.Replace("&amp;", "&");
            }
            #endregion

            editor1.Render(this.Options.Fields, base.FieldInfo, parent);
            if (Context.ClientPage.IsEvent)
            {
                SheerResponse.SetInnerHtml("ContentEditor", parent);
            }
        }
        protected void ToggleSection(string sectionName, string collapsed)
        {
            Assert.ArgumentNotNull(sectionName, "sectionName");
            Assert.ArgumentNotNull(collapsed, "collapsed");
            this.Options = Sitecore.Shell.Applications.ContentManager.FieldEditorOptions.Parse(new UrlString(WebUtil.GetQueryString()));
            ClientPipelineArgs currentPipelineArgs = Context.ClientPage.CurrentPipelineArgs as ClientPipelineArgs;
            Assert.IsNotNull(currentPipelineArgs, typeof(ClientPipelineArgs));
            if (currentPipelineArgs.IsPostBack)
            {
                if (currentPipelineArgs.Result == "no")
                {
                    return;
                }
                currentPipelineArgs.IsPostBack = false;
            }
            else if (collapsed == "0")
            {
                Pair<ValidatorResult, Sitecore.Data.Validators.BaseValidator> pair1 = this.ValidateSection(sectionName);
                ValidatorResult result = pair1.Part1;
                Sitecore.Data.Validators.BaseValidator failedValidator = pair1.Part2;
                if ((failedValidator != null) && failedValidator.IsEvaluating)
                {
                    SheerResponse.Alert(Translate.Text("The fields in this section are currently being validated.\n\nYou must wait for validation to complete before you can collapse this section."), Array.Empty<string>());
                    currentPipelineArgs.AbortPipeline();
                    return;
                }
                if (result == ValidatorResult.CriticalError)
                {
                    string text = Translate.Text("Some of the fields in this section contain critical errors.\n\nThe fields in this section will not be revalidated if you save the current item while this section is collapsed.\nAre you sure you want to collapse this section?");
                    if (MainUtil.GetBool(currentPipelineArgs.CustomData["showvalidationdetails"], false) && (failedValidator != null))
                    {
                        text = text + ValidatorManager.GetValidationErrorDetails(failedValidator);
                    }
                    SheerResponse.Confirm(text);
                    currentPipelineArgs.WaitForPostBack();
                    return;
                }
                if (result == ValidatorResult.FatalError)
                {
                    string text = Translate.Text("Some of the fields in this section contain fatal errors.\n\nYou must resolve these errors before you can collapse this section.");
                    if (MainUtil.GetBool(currentPipelineArgs.CustomData["showvalidationdetails"], false) && (failedValidator != null))
                    {
                        text = text + ValidatorManager.GetValidationErrorDetails(failedValidator);
                    }
                    SheerResponse.Alert(text, Array.Empty<string>());
                    currentPipelineArgs.AbortPipeline();
                    return;
                }
            }
            if (collapsed == "0")
            {
                this.SaveFieldValuesInHandler(sectionName);
            }
            Registry.SetString("/Current_User/Content Editor/Sections/Collapsed", new UrlString(Registry.GetString("/Current_User/Content Editor/Sections/Collapsed")) { [sectionName] = (collapsed == "1") ? "0" : "1" }.ToString());
            this.SectionToggling = true;
        }
        private bool SectionToggling { get; set; }
    }
}