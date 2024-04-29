﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tuvi.Core.DataStorage;
using Tuvi.Core.Entities;

namespace Tuvi.Core.Impl.CredentialsManagement
{
    public static class CredentialsManagerCreator
    {
        public static ICredentialsManager GetCredentialsProvider(IDataStorage storage, ITokenResolver tokenResolver)
        {
            return new CredentialsManager(storage, tokenResolver);
        }
    }

    internal class CredentialsManager : ICredentialsManager
    {
        private readonly ITokenResolver _tokenResolver;
        private readonly IDataStorage _storage;

        internal CredentialsManager(IDataStorage storage, ITokenResolver tokenResolver)
        {
            _tokenResolver = tokenResolver;
            _storage = storage;
        }

        public ICredentialsProvider CreateCredentialsProvider(Account account)
        {
            ICredentialsProvider provider = null;
            switch (account?.AuthData)
            {
                case BasicAuthData basicAuthData:

                    provider = new BasicCredentialsProvider()
                    {
                        BasicCredentials = new BasicCredentials()
                        {
                            UserName = account.Email.Address,
                            Password = basicAuthData.Password
                        }
                    };

                    break;
                case OAuth2Data data:

                    _tokenResolver.AddOrUpdateToken(account.Email, data.AuthAssistantId, data.RefreshToken);

                    provider = new OAuth2CredentialsProvider(_storage, account, data)
                    {
                        TokenResolver = (EmailAddress address, CancellationToken ct) =>_tokenResolver.GetAccessTokenAsync(address, ct)
                    };

                    break;

                case ProtonAuthData protonData:

                    provider = new ProtonCredentialsProvider()
                    {
                        Credentials = new ProtonCredentials()
                        {
                            UserName = account.Email.Address,
                            UserId = protonData.UserId,
                            RefreshToken = protonData.RefreshToken,
                            SaltedPassword = protonData.SaltedPassword
                        }
                    };
                    break;
            }

            return provider;
        }

        internal class BasicCredentialsProvider : ICredentialsProvider
        {
            public BasicCredentials BasicCredentials { get; set; }

            public Task<AccountCredentials> GetCredentialsAsync(HashSet<string> supportedAuthMechanisms, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<AccountCredentials>(BasicCredentials);
            }
        }

        internal class OAuth2CredentialsProvider : ICredentialsProvider
        {
            private readonly IDataStorage _storage;
            private readonly EmailAddress _emailAddress;

            private Account _newAccount;
            private string _refreshToken;

            internal Func<EmailAddress, CancellationToken, Task<(string accessToken, string newRefreshToken)>> TokenResolver { get; set; }

            public OAuth2CredentialsProvider(IDataStorage storage, Account newAccount, OAuth2Data data)
            {
                if (data == null)
                {
                    throw new ArgumentNullException(nameof(data));
                }

                _storage = storage ?? throw new ArgumentNullException(nameof(storage));
                _newAccount = newAccount ?? throw new ArgumentNullException(nameof(newAccount));
                _emailAddress = _newAccount.Email;
                _refreshToken = data.RefreshToken;
            }

            public async Task<AccountCredentials> GetCredentialsAsync(HashSet<string> supportedAuthMechanisms, CancellationToken cancellationToken = default)
            {
                return new OAuth2Credentials()
                {
                    UserName = _emailAddress.Address,
                    AccessToken = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false)
                };
            }

            private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
            {
                (string accessToken, string refreshToken) = await TokenResolver(_emailAddress, cancellationToken).ConfigureAwait(false);

                string oldRefreshToken = _refreshToken;
                _refreshToken = refreshToken;

                if (_newAccount != null || !_refreshToken.Equals(oldRefreshToken, StringComparison.Ordinal))
                {
                    await UpdateAccount(_refreshToken, cancellationToken).ConfigureAwait(false);
                }

                return accessToken;
            }

            private async Task UpdateAccount(string refreshToken, CancellationToken cancellationToken = default)
            {
                try
                {
                    Account account = await _storage.GetAccountAsync(_emailAddress, cancellationToken).ConfigureAwait(false);
                    UpdateAccount(account, refreshToken);
                    await _storage.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
                    _newAccount = null;
                }
                catch (Exception exception) when (exception is AccountIsNotExistInDatabaseException)
                {
                    // The account is not in storage at the time of creation
                    UpdateAccount(_newAccount, refreshToken);
                }
            }

            private void UpdateAccount(Account account, string refreshToken)
            {
                if (account?.AuthData is OAuth2Data oauth2Data)
                {
                    oauth2Data.RefreshToken = refreshToken;
                }
                else if (account != null)
                {
                    throw new AuthenticationException(account.Email, "Account doesn't have authentication data", null);
                }
                else
                {
                    throw new AuthenticationException(_emailAddress, "Account not found", null);
                }
            }
        }

        internal class ProtonCredentialsProvider : ICredentialsProvider
        {
            public ProtonCredentials Credentials { get; set; }

            public Task<AccountCredentials> GetCredentialsAsync(HashSet<string> supportedAuthMechanisms, CancellationToken cancellationToken = default)
            {
                return Task.FromResult((AccountCredentials)Credentials);
            }
        }
    }
}
