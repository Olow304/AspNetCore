// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// A base class for <see cref="IUriHelper"/> implementations.
    /// </summary>
    public abstract class UriHelperBase : IUriHelper
    {
        private EventHandler<LocationChangedEventArgs> _onLocationChanged;

        /// <summary>
        /// An event that fires when the navigation location has changed.
        /// </summary>
        public event EventHandler<LocationChangedEventArgs> OnLocationChanged
        {
            add
            {
                AssertInitialized();
                _onLocationChanged += value;
            }
            remove
            {
                _onLocationChanged -= value;
            }
        }

        // For the baseUri it's worth storing both the string form and Uri form and
        // keeping them in sync. These are always represented as absolute URIs with
        // a trailing slash.
        private Uri _baseUri;
        private string _baseUriString;

        // The URI. Always represented an absolute URI.
        private string _uri;

        private bool _isInitialized;

        /// <summary>
        /// Navigates to the specified URI.
        /// </summary>
        /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
        /// (as returned by <see cref="GetBaseUri"/>).</param>
        public void NavigateTo(string uri)
        {
            NavigateTo(uri, forceLoad: false);
        }

        /// <summary>
        /// Navigates to the specified URI.
        /// </summary>
        /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
        /// (as returned by <see cref="GetBaseUri"/>).</param>
        /// <param name="forceLoad">If true, bypasses client-side routing and forces the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.</param>
        public void NavigateTo(string uri, bool forceLoad)
        {
            AssertInitialized();
            NavigateToCore(uri, forceLoad);
        }

        /// <summary>
        /// Navigates to the specified URI.
        /// </summary>
        /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
        /// (as returned by <see cref="GetBaseUri"/>).</param>
        /// <param name="forceLoad">If true, bypasses client-side routing and forces the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.</param>
        protected abstract void NavigateToCore(string uri, bool forceLoad);

        /// <summary>
        /// Called to initialize BaseURI and current URI before these values are used for the first time.
        /// Override this method to dynamically calculate these values.
        /// </summary>
        public virtual void InitializeState(string uriAbsolute, string baseUriAbsolute)
        {
            if (uriAbsolute == null)
            {
                throw new ArgumentNullException(nameof(uriAbsolute));
            }

            if (baseUriAbsolute == null)
            {
                throw new ArgumentNullException(nameof(baseUriAbsolute));
            }

            if (_isInitialized)
            {
                throw new InvalidOperationException($"'{typeof(UriHelperBase).Name}' already initialized.");
            }
            _isInitialized = true;

            SetAbsoluteUri(uriAbsolute);
            SetAbsoluteBaseUri(baseUriAbsolute);
        }

        /// <summary>
        /// Allows derived classes to lazyly self initialize. It does nothing unless overriden.
        /// </summary>
        protected virtual void EnsureInitialized()
        {
        }

        /// <summary>
        /// Gets the current absolute URI.
        /// </summary>
        /// <returns>The current absolute URI.</returns>
        public string GetAbsoluteUri()
        {
            AssertInitialized();
            return _uri;
        }

        /// <summary>
        /// Gets the base URI (with trailing slash) that can be prepended before relative URI paths to
        /// produce an absolute URI. Typically this corresponds to the 'href' attribute on the
        /// document's &lt;base&gt; element.
        /// </summary>
        /// <returns>The URI prefix, which has a trailing slash.</returns>
        public virtual string GetBaseUri()
        {
            AssertInitialized();
            return _baseUriString;
        }

        /// <summary>
        /// Converts a relative URI into an absolute one (by resolving it
        /// relative to the current absolute URI).
        /// </summary>
        /// <param name="href">The relative URI.</param>
        /// <returns>The absolute URI.</returns>
        public Uri ToAbsoluteUri(string href)
        {
            AssertInitialized();
            return new Uri(_baseUri, href);
        }

        /// <summary>
        /// Given a base URI (e.g., one previously returned by <see cref="GetBaseUri"/>),
        /// converts an absolute URI into one relative to the base URI prefix.
        /// </summary>
        /// <param name="baseUri">
        /// The base URI prefix (e.g., previously returned by <see cref="GetBaseUri"/>).
        /// </param>
        /// <param name="locationAbsolute">An absolute URI that is within the space of the base URI.</param>
        /// <returns>A relative URI path.</returns>
        public string ToBaseRelativePath(string baseUri, string locationAbsolute)
        {
            if (locationAbsolute.StartsWith(baseUri, StringComparison.Ordinal))
            {
                // The absolute URI must be of the form "{baseUri}something" (where
                // baseUri ends with a slash), and from that we return "something"
                return locationAbsolute.Substring(baseUri.Length);
            }
            else if ($"{locationAbsolute}/".Equals(baseUri, StringComparison.Ordinal))
            {
                // Special case: for the base URI "/something/", if you're at
                // "/something" then treat it as if you were at "/something/" (i.e.,
                // with the trailing slash). It's a bit ambiguous because we don't know
                // whether the server would return the same page whether or not the
                // slash is present, but ASP.NET Core at least does by default when
                // using PathBase.
                return string.Empty;
            }

            var message = $"The URI '{locationAbsolute}' is not contained by the base URI '{baseUri}'.";
            throw new ArgumentException(message);
        }

        /// <summary>
        /// Set the URI to the provided value.
        /// </summary>
        /// <param name="uri">The URI. Must be an absolute URI.</param>
        /// <remarks>
        /// Calling <see cref="SetAbsoluteUri(string)"/> does not trigger <see cref="OnLocationChanged"/>.
        /// </remarks>
        protected void SetAbsoluteUri(string uri)
        {
            _uri = uri;
        }

        /// <summary>
        /// Sets the base URI to the provided value (after normalization).
        /// </summary>
        /// <param name="baseUri">The base URI. Must be an absolute URI.</param>
        /// <remarks>
        /// Calling <see cref="SetAbsoluteBaseUri(string)"/> does not trigger <see cref="OnLocationChanged"/>.
        /// </remarks>
        protected void SetAbsoluteBaseUri(string baseUri)
        {
            if (baseUri != null)
            {
                var lastSlashIndex = baseUri.LastIndexOf('/');
                if (lastSlashIndex >= 0)
                {
                    baseUri = baseUri.Substring(0, lastSlashIndex + 1);
                }
            }

            _baseUriString = baseUri ?? "/";
            _baseUri = new Uri(_baseUriString);
        }

        /// <summary>
        /// Triggers the <see cref="OnLocationChanged"/> event with the current URI value.
        /// </summary>
        protected void TriggerOnLocationChanged(bool isinterceptedLink)
        {
            _onLocationChanged?.Invoke(this, new LocationChangedEventArgs(_uri, isinterceptedLink));
        }

        private void AssertInitialized()
        {
            if (!_isInitialized)
            {
                EnsureInitialized();
            }

            if (!_isInitialized)
            {
                throw new InvalidOperationException($"'{GetType().Name}' has not been initialized.");
            }
        }
    }
}
