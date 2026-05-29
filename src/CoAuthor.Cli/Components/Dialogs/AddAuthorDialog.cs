using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAuthor.Cli.Components.Dialogs;

public sealed class AddAuthorDialog : Dialog<GitCoAuthor>
{
    private GitCoAuthor _gitCoAuthor;
    
    public AddAuthorDialog()
    {
        _gitCoAuthor = new GitCoAuthor();
        
        Label nameLabel = new()
        {
            Text = $"{Resources.Labels_Input_Name}:"
        };

        TextField nameText = new()
        {
            X = Pos.Right(nameLabel) + 1,
            
            Width = Dim.Fill(),
        };

        Label emailAddressLabel = new()
        {
            Text = $"{Resources.Labels_Input_Email}:",
            X = Pos.Left(nameLabel), 
            Y = Pos.Bottom(nameLabel) + 1 
        };

        TextField emailAddressText = new()
        {
            X = Pos.Left(emailAddressLabel),
            Y = Pos.Top(nameText),
            Width = Dim.Fill(),
        };

        Label coAuthorTypeLabel = new()
        {
            Text = $"{Resources.Labels_Input_ContributorType}:",
            X = Pos.Left(emailAddressLabel),
            Y = Pos.Bottom(emailAddressLabel) + 1,
        };
        
        OptionSelector<ContributorType> coAuthorTypeSelector = new()
        {
            X = Pos.Left(emailAddressLabel),
            Y = Pos.Bottom(emailAddressText) + 1,
            Labels = [Resources.Labels_AuthorTypes_Values_Agent, 
                /*Resources.AuthorTypes_Values_AI_Other,*/
                Resources.Labels_AuthorTypes_Values_Human]
        };

        Label defaultAttributionTypeLabel = new()
        {
            Text = "Default Attribution Type:",
            X = Pos.Left(emailAddressLabel),
            Y = Pos.Bottom(coAuthorTypeLabel) + 1,
        };
        
        DropDownList<AttributionType> defaultAttributionType = new()
        {
            X = Pos.Left(emailAddressText),
            Y = Pos.Bottom(coAuthorTypeSelector) + 1,
        };
        
        Button cancelButton = new()
        {
            Text = Resources.Labels_Buttons_Cancel,
            X = Pos.Center() - 1,
            IsDefault = false,
        };
        Button addButton = new()
        {
            Text = Resources.Labels_Buttons_SaveAuthor,
            X = Pos.Center() + 1,
            IsDefault = true,
        };

        cancelButton.Accepting += (sender, args) =>
        {
            App?.RequestStop();
            args.Handled = true;
        };

        addButton.Accepting += (sender, args) =>
        {
            if(!string.IsNullOrEmpty(nameText.Text))
                _gitCoAuthor.Name = nameText.Text;
            else
                
            
            if(!string.IsNullOrEmpty(emailAddressText.Text))
                _gitCoAuthor.Email = emailAddressText.Text;

            if (coAuthorTypeSelector.Value is not null)
                _gitCoAuthor.Type = (ContributorType)coAuthorTypeSelector.Value;
            
            if(defaultAttributionType.Value is not null)
                _gitCoAuthor.DefaultAttributionType = (AttributionType)defaultAttributionType.Value;
        };
        
        Add(nameLabel, nameText, emailAddressLabel, emailAddressText,
            coAuthorTypeLabel, coAuthorTypeSelector, defaultAttributionTypeLabel, defaultAttributionType,
            cancelButton, addButton);
    }
    
    protected override bool OnAccepting (CommandEventArgs args)
    {
        /*if (base.OnAccepting (args))
        {
            return true;
        }*/

        if (!string.IsNullOrEmpty(_gitCoAuthor.Name) && !string.IsNullOrEmpty(_gitCoAuthor.Email)
                                                     && _gitCoAuthor.Type != ContributorType.NotDefined)
        {
            Result = _gitCoAuthor;
            RequestStop();
            return true;
        }
        
        return false;
    }
}