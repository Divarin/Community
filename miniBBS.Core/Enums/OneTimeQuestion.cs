namespace miniBBS.Core.Enums
{
    /// <summary>
    /// Don't delete any entries from this even if the question is no longer valid because it will cause a json parsing issue in OneTimeQuestions
    /// </summary>
    public enum OneTimeQuestion
    {
        SetUserWebPref,
        LoginStartupMode
    }
}
