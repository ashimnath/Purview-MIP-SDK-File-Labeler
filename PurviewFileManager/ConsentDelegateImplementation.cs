using Microsoft.InformationProtection;

namespace PurviewFileManager;

public class ConsentDelegateImplementation : IConsentDelegate
{
    public Consent GetUserConsent(string url)
    {
        return Consent.Accept;
    }
}