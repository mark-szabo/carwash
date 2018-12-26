using System;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using MSHU.CarWash.Bot.Dialogs.ConfirmDropoff;

namespace MSHU.CarWash.Bot.States
{
    /// <summary>
    /// This class is created as a Singleton and passed into the IBot-derived constructor.
    /// </summary>
    public class StateAccessors
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateAccessors"/> class.
        /// Contains the <see cref="ConversationState"/> and associated <see cref="IStatePropertyAccessor{T}"/>.
        /// </summary>
        /// <param name="conversationState">The state object that stores the dialog state.</param>
        /// <param name="userState">The state object that stores the user state.</param>
        public StateAccessors(ConversationState conversationState, UserState userState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));

            UserProfileAccessor = userState.CreateProperty<UserProfile>(nameof(UserProfile));
            DialogStateAccessor = conversationState.CreateProperty<DialogState>(nameof(DialogState));
            ConfirmDropoffStateAccessor = conversationState.CreateProperty<ConfirmDropoffState>(nameof(ConfirmDropoffState));
        }

        /// <summary>
        /// Gets or sets the <see cref="IStatePropertyAccessor{T}"/> for UserProfile.
        /// </summary>
        /// <value>
        /// The accessor stores the user state for the user's profile.
        /// </value>
        public IStatePropertyAccessor<UserProfile> UserProfileAccessor { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IStatePropertyAccessor{T}"/> for DialogState.
        /// </summary>
        /// <value>
        /// The accessor stores the dialog state for the conversation.
        /// </value>
        public IStatePropertyAccessor<DialogState> DialogStateAccessor { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IStatePropertyAccessor{T}"/> for ConfirmDropoffState.
        /// </summary>
        /// <value>
        /// The accessor stores the dialog state for the ConfirmDropoff dialog.
        /// </value>
        public IStatePropertyAccessor<ConfirmDropoffState> ConfirmDropoffStateAccessor { get; set; }

        /// <summary>
        /// Gets the <see cref="ConversationState"/> object for the conversation.
        /// </summary>
        /// <value>The <see cref="ConversationState"/> object.</value>
        public ConversationState ConversationState { get; }

        /// <summary>
        /// Gets the <see cref="UserState"/> object for the conversation.
        /// </summary>
        /// <value>The <see cref="UserState"/> object.</value>
        public UserState UserState { get; }
    }
}