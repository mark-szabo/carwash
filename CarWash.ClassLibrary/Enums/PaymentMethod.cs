namespace CarWash.ClassLibrary.Enums
{
    /// <summary>
    /// Represents the available methods of payment for a transaction.
    /// </summary>
    public enum PaymentMethod
    {
        /// <summary>
        /// Payment method not set.
        /// </summary>
        NotSet = 0,

        /// <summary>
        /// Payment via credit card.
        /// </summary>
        CreditCard = 1,

        /// <summary>
        /// Payment via wire transfer.
        /// </summary>
        WireTransfer = 2,
    }
}
