/*
 * Copyright 2016 Mikko Teräs and Niilo Säämänen.
 *
 * This file is part of Auremo.
 *
 * Auremo is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software
 * Foundation, version 2.
 *
 * Auremo is distributed in the hope that it will be useful, but WITHOUT ANY
 * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
 * A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with Auremo. If not, see http://www.gnu.org/licenses/.
 */

namespace Auremo.MusicLibrary
{
    /// <summary>
    /// A Playable is a leaf node in the library hierarchy. It is something that
    /// MPD knows how to play, i.e., something that has a filename, URL, path, etc.
    /// </summary>
    public interface Playable
    {
        Path Path { get; }
        string Title { get; }
    }
}
