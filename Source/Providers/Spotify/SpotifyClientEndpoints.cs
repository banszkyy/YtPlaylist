using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using JsonExtensions.Http;

namespace YtPlaylist.Spotify;

partial class SpotifyClient
{
    public async Task<ImmutableArray<SearchResultItem>> Search(string query, int offset = 0, int limit = 50, CancellationToken cancellationToken = default)
    {
        PathfinderResponse v = await PathfinderRequest<PathfinderResponse>(new PathfinderRequest()
        {
            OperationName = "searchTopResultsList",
            Extensions = new RequestExtensions()
            {
                PersistedQuery = new PersistedQuery()
                {
                    Sha256Hash = "337d8b1b4f911fb12c60996623391703c2807550baccb51d95f5eabc8c8bdacd",
                    Version = 1,
                }
            },
            Variables = new PathfinderVariables()
            {
                Query = query,
                Offset = offset,
                Limit = limit,
                IncludeAlbumPreReleases = true,
                IncludeArtistHasConcertsField = false,
                IncludeAudiobooks = true,
                IncludeAuthors = false,
                IncludeEpisodeContentRatingsV2 = true,
                IncludePreReleases = true,
                IsPrefix = null,
                NumberOfTopResults = limit,
                SectionFilters = [
                    "GENERIC",
                    "VIDEO_CONTENT"
                ],
            }
        }, true, new() { { "q", query }, { "o", offset.ToString() }, { "l", limit.ToString() } }, cancellationToken);

        return [.. v.Data.Search?.TopResults?.Items.Select(v => v.Item.Data) ?? throw new UnreachableException()];
    }

    public async Task<ImmutableArray<SearchResultItems>> SearchTracks(string query, int offset = 0, int limit = 50, CancellationToken cancellationToken = default)
    {
        PathfinderResponse v = await PathfinderRequest<PathfinderResponse>(new PathfinderRequest()
        {
            OperationName = "searchTracks",
            Extensions = new RequestExtensions()
            {
                PersistedQuery = new PersistedQuery()
                {
                    Sha256Hash = "59ee4a659c32e9ad894a71308207594a65ba67bb6b632b183abe97303a51fa55",
                    Version = 1,
                }
            },
            Variables = new PathfinderVariables()
            {
                SearchTerm = query,
                Offset = offset,
                Limit = limit,
                IncludeAlbumPreReleases = true,
                IncludeAudiobooks = true,
                IncludeAuthors = false,
                IncludeEpisodeContentRatingsV2 = true,
                IncludePreReleases = true,
                NumberOfTopResults = limit,
            }
        }, true, new() { { "q", query }, { "o", offset.ToString() }, { "l", limit.ToString() } }, cancellationToken);

        return [.. v.Data.Search?.Tracks?.Items ?? throw new UnreachableException()];
    }

    public async Task<Library> FetchLibrary(string[]? features = null, string[]? filters = null, int offset = 0, int limit = 50, string? textFilter = null, CancellationToken cancellationToken = default)
    {
        features ??= [
            "LIKED_SONGS",
            "YOUR_EPISODES_V2",
            "PRERELEASES",
            "PRERELEASES_V2",
            "CLIPS",
            "EVENTS",
        ];
        filters ??= [];
        textFilter ??= string.Empty;

        MeResponse v = await PathfinderRequest<MeResponse>(new PathfinderRequest()
        {
            OperationName = "libraryV3",
            Extensions = new RequestExtensions()
            {
                PersistedQuery = new PersistedQuery()
                {
                    Sha256Hash = "390c78e5b951029bad359785e69b07b536a509c581cbcd0aded5e5067f187455",
                    Version = 1,
                }
            },
            Variables = new PathfinderVariables()
            {
                ExpandedFolders = [],
                Features = features,
                Filters = filters,
                Flatten = false,
                FolderUri = null,
                IncludeFoldersWhenFlattening = true,
                Limit = limit,
                Offset = offset,
                Order = null,
                TextFilter = textFilter,
            }
        }, false, null, cancellationToken);

        return v.Data.Me.Library ?? throw new UnreachableException();
    }

    public async Task<Playlist> FetchPlaylistMetadata(string uri, CancellationToken cancellationToken = default)
    {
        PathfinderResponse v = await PathfinderRequest<PathfinderResponse>(new PathfinderRequest()
        {
            OperationName = "searchTracks",
            Extensions = new RequestExtensions()
            {
                PersistedQuery = new PersistedQuery()
                {
                    Sha256Hash = "59ee4a659c32e9ad894a71308207594a65ba67bb6b632b183abe97303a51fa55",
                    Version = 1,
                }
            },
            Variables = new PathfinderVariables()
            {
                Uri = uri,
                Offset = 0,
                Limit = 100,
                EnableWatchFeedEntrypoint = true,
            }
        }, false, null, cancellationToken);

        return v.Data.Playlist ?? throw new UnreachableException();
    }

    public async Task DeletePlaylist(string userHash, string uri, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"https://spclient.wg.spotify.com/playlist/v2/user/{userHash}/rootlist/changes", UriKind.Absolute));
        request.Version = new Version(2, 0);
        request.Headers.Clear();
        PrepareXHRRequest(request);
        request.Content = JsonContent.Create(new DeltaRequest()
        {
            Deltas = [
                new()
                {
                    Operations = [
                        new()
                        {
                            Kind = "REM",
                            Remove = new()
                            {
                                Items = [
                                    new() { Uri = uri },
                                ],
                                ItemsAsKey = true,
                            },
                        },
                    ],
                    Info = new()
                    {
                        Source = new()
                        {
                            Client = "WEBPLAYER",
                        },
                    },
                },
            ],
        });
        HttpResponseMessage res = await client.SendAsync(request, cancellationToken);
        res.EnsureSuccessStatusCode();
    }

    public async Task<MeProfile> FetchProfileAttributes(CancellationToken cancellationToken = default)
    {
        MeResponse v = await PathfinderRequest<MeResponse>(new PathfinderRequest()
        {
            OperationName = "profileAttributes",
            Extensions = new RequestExtensions()
            {
                PersistedQuery = new PersistedQuery()
                {
                    Sha256Hash = "08ffb4730af3746e04a8301396f20875dbbce10c75243803091a9274eacc8ac0",
                    Version = 1,
                }
            },
            Variables = new PathfinderVariables()
            {
            }
        }, false, null, cancellationToken);

        return v.Data.Me.Profile ?? throw new UnreachableException();
    }

    public async Task<CreatePlaylistResponse> CreatePlaylist(string playlistName, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"https://spclient.wg.spotify.com/playlist/v2/playlist", UriKind.Absolute));
        request.Version = new Version(2, 0);
        request.Headers.Clear();
        PrepareXHRRequest(request);
        request.Content = JsonContent.Create(new OperationsRequest()
        {
            Operations = [
                new OperationItem2()
                {
                    Kind = "UPDATE_LIST_ATTRIBUTES",
                    UpdateListAttributes = new()
                    {
                        NewAttributes = new()
                        {
                            Values = new()
                            {
                                Name = playlistName,
                            },
                        },
                    },
                },
            ],
        });
        HttpResponseMessage res = await client.SendAsync(request, cancellationToken);
        res.EnsureSuccessStatusCode();
        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken);
        return body.Deserialize<CreatePlaylistResponse>() ?? throw new JsonException();
    }

    public async Task PublishPlaylist(string username, string playlistUri, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"https://spclient.wg.spotify.com/playlist/v2/user/{username}/rootlist/changes", UriKind.Absolute));
        request.Version = new Version(2, 0);
        request.Headers.Clear();
        PrepareXHRRequest(request);
        request.Content = JsonContent.Create(new DeltaRequest()
        {
            Deltas = [
                new()
                {
                    Info = new()
                    {
                        Source = new()
                        {
                            Client = "WEBPLAYER",
                        },
                    },
                    Operations = [
                        new()
                        {
                            Kind = "ADD",
                            Add = new()
                            {
                                AddFirst = true,
                                Items = [
                                    new()
                                    {
                                        Attributes = new()
                                        {
                                            { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() },
                                        },
                                        Uri = playlistUri,
                                    },
                                ],
                            },
                        },
                    ],
                },
            ],
        });
        HttpResponseMessage res = await client.SendAsync(request, cancellationToken);
        res.EnsureSuccessStatusCode();
    }

    public async Task SetPlaylistVisibility(string username, string playlistUri, bool isPublic, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"https://spclient.wg.spotify.com/playlist/v2/user/{username}/rootlist/changes", UriKind.Absolute));
        request.Version = new Version(2, 0);
        request.Headers.Clear();
        PrepareXHRRequest(request);
        request.Content = JsonContent.Create(new DeltaRequest()
        {
            Deltas = [
                new()
                {
                    Info = new()
                    {
                        Source = new()
                        {
                            Client = "WEBPLAYER",
                        },
                    },
                    Operations = [
                        new()
                        {
                            Kind = "UPDATE_ITEM_ATTRIBUTES",
                            UpdateItemAttributes = new()
                            {
                                Item = new() { Uri = playlistUri },
                                NewAttributes = new()
                                {
                                    Values = new()
                                    {
                                        { "public", isPublic },
                                    },
                                },
                            },
                        },
                    ],
                },
            ],
        });
        HttpResponseMessage res = await client.SendAsync(request, cancellationToken);
        res.EnsureSuccessStatusCode();
    }

    public async Task SetPlaylistDescription(string playlistId, string description, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"https://spclient.wg.spotify.com/playlist/v2/playlist/{playlistId}/changes", UriKind.Absolute));
        request.Version = new Version(2, 0);
        request.Headers.Clear();
        PrepareXHRRequest(request);
        request.Content = JsonContent.Create(new DeltaRequest()
        {
            Deltas = [
                new()
                {
                    Info = new()
                    {
                        Source = new()
                        {
                            Client = "WEBPLAYER",
                        },
                    },
                    Operations = [
                        new()
                        {
                            Kind = "UPDATE_LIST_ATTRIBUTES",
                            UpdateListAttributes = new()
                            {
                                NewAttributes = new()
                                {
                                    Values = new()
                                    {
                                        { "description", description },
                                    },
                                },
                            },
                        },
                    ],
                },
            ],
        });
        HttpResponseMessage res = await client.SendAsync(request, cancellationToken);
        res.EnsureSuccessStatusCode();
    }

    public async Task AddToPlaylist(string playlistUri, IEnumerable<string> trackUris, CancellationToken cancellationToken = default)
    {
        await PathfinderRequest(new PathfinderRequest()
        {
            OperationName = "addToPlaylist",
            Extensions = new RequestExtensions()
            {
                PersistedQuery = new PersistedQuery()
                {
                    Sha256Hash = "47b2a1234b17748d332dd0431534f22450e9ecbb3d5ddcdacbd83368636a0990",
                    Version = 1,
                }
            },
            Variables = new PathfinderVariables()
            {
                PlaylistUri = playlistUri,
                NewPosition = new()
                {
                    MoveType = "BOTTOM_OF_PLAYLIST",
                    FromUid = null,
                },
                PlaylistItemUris = [.. trackUris],
            }
        }, false, null, cancellationToken);
    }

    public async Task RemoveFromPlaylist(string playlistUri, IEnumerable<string> trackUris, CancellationToken cancellationToken = default)
    {
        await PathfinderRequest(new PathfinderRequest()
        {
            OperationName = "removeFromPlaylist",
            Extensions = new RequestExtensions()
            {
                PersistedQuery = new PersistedQuery()
                {
                    Sha256Hash = "47b2a1234b17748d332dd0431534f22450e9ecbb3d5ddcdacbd83368636a0990",
                    Version = 1,
                }
            },
            Variables = new PathfinderVariables()
            {
                PlaylistUri = playlistUri,
                Uids = [.. trackUris],
            }
        }, false, null, cancellationToken);
    }

    public async Task<Content> FetchPlaylistContents(string playlistUri, int offset = 0, int limit = 50, CancellationToken cancellationToken = default)
    {
        PathfinderResponse v = await PathfinderRequest<PathfinderResponse>(new PathfinderRequest()
        {
            OperationName = "fetchPlaylistContents",
            Extensions = new RequestExtensions()
            {
                PersistedQuery = new PersistedQuery()
                {
                    Sha256Hash = "e4b2953f160e58e38ac025d79b5a9b3aceee5c4c716598e9830bfceb69faff5f",
                    Version = 1,
                }
            },
            Variables = new PathfinderVariables()
            {
                IncludeEpisodeContentRatingsV2 = true,
                Limit = limit,
                Offset = offset,
                Uri = playlistUri,
            }
        }, false, null, cancellationToken);
        return v.Data.Playlist?.Content ?? throw new UnreachableException();
    }

    public async IAsyncEnumerable<ContentItem> FetchPlaylistContents(string playlistUri, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        long offset = 0;
        while (true)
        {
            PathfinderResponse v = await PathfinderRequest<PathfinderResponse>(new PathfinderRequest()
            {
                OperationName = "fetchPlaylistContents",
                Extensions = new RequestExtensions()
                {
                    PersistedQuery = new PersistedQuery()
                    {
                        Sha256Hash = "e4b2953f160e58e38ac025d79b5a9b3aceee5c4c716598e9830bfceb69faff5f",
                        Version = 1,
                    }
                },
                Variables = new PathfinderVariables()
                {
                    IncludeEpisodeContentRatingsV2 = true,
                    Limit = 50,
                    Offset = offset,
                    Uri = playlistUri,
                }
            }, false, null, cancellationToken);

            foreach (ContentItem item in v.Data.Playlist.ThrowIfNull().Content.ThrowIfNull().Items.ThrowIfNull()) yield return item;

            if (offset + 50 >= v.Data.Playlist.Content.ThrowIfNull().TotalCount) break;

            offset = v.Data.Playlist.Content.PagingInfo.ThrowIfNull().NextOffset.ThrowIfNull();
        }
    }
}
