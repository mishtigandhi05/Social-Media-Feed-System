#include <iostream>
#include <string>
#include <sstream>

using namespace std;

// Post Node
struct Post
{
    int postId;
    string content;
    Post* prev;
    Post* next;
    Post* globalNext;

    Post(int pid, const string& c)
        : postId(pid), content(c), prev(nullptr), next(nullptr), globalNext(nullptr)
    {
    }
};

// User Node
struct UserNode
{
    string userName;
    string password;
    Post* firstPost;
    UserNode* next;

    UserNode(const string& name, const string& pass)
        : userName(name), password(pass), firstPost(nullptr), next(nullptr)
    {
    }
};

// Hash Map Entry
struct HashEntry
{
    string userName;
    UserNode* userPtr;
    Post* lastPost;
    HashEntry* next;

    HashEntry(const string& name, UserNode* uptr, Post* lptr)
        : userName(name), userPtr(uptr), lastPost(lptr), next(nullptr)
    {
    }
};

// Simple chained-hash map for users -> lastPost + user pointer
class HashMap
{
private:
    static const int SIZE = 100;
    HashEntry* table[SIZE];

    int hash(const string& key) const
    {
        int h = 0;
        for (char c : key)
        {
            h = (h * 31 + static_cast<unsigned char>(c)) % SIZE;
        }
        return h;
    }

public:
    HashMap()
    {
        for (int i = 0; i < SIZE; ++i) table[i] = nullptr;
    }

    // Insert or update
    void put(const string& userName, UserNode* uptr, Post* lptr)
    {
        int index = hash(userName);
        HashEntry* entry = table[index];
        while (entry)
        {
            if (entry->userName == userName)
            {
                entry->userPtr = uptr;
                entry->lastPost = lptr;
                return;
            }
            entry = entry->next;
        }
        HashEntry* newEntry = new HashEntry(userName, uptr, lptr);
        newEntry->next = table[index];
        table[index] = newEntry;
    }

    HashEntry* get(const string& userName) const
    {
        int index = hash(userName);
        HashEntry* entry = table[index];
        while (entry)
        {
            if (entry->userName == userName) return entry;
            entry = entry->next;
        }
        return nullptr;
    }
};

// Social Media
class SocialMedia
{
private:
    UserNode* usersHead;
    HashMap map;
    Post* globalHead;

    bool authenticate(const string& userName, const string& password) const
    {
        HashEntry* entry = map.get(userName);
        if (!entry) return false;
        return entry->userPtr->password == password;
    }

public:
    SocialMedia() : usersHead(nullptr), globalHead(nullptr) {}

    void createUser(const string& userName, const string& password)
    {
        if (map.get(userName))
        {
            cout << "User " << userName << " already exists.\n";
            return;
        }
        UserNode* newUser = new UserNode(userName, password);
        newUser->next = usersHead;
        usersHead = newUser;
        map.put(userName, newUser, nullptr);
        cout << "User created: " << userName << "\n";
    }

    void addPost(const string& userName, const string& password, int postId, const string& content)
    {
        if (!authenticate(userName, password))
        {
            cout << "Auth failed for user " << userName << "\n";
            return;
        }

        HashEntry* entry = map.get(userName);
        if (!entry) // should not happen if authenticate passed, but guard anyway
        {
            cout << "User entry missing for " << userName << "\n";
            return;
        }

        UserNode* user = entry->userPtr;
        Post* newPost = new Post(postId, content);

        Post* last = entry->lastPost;
        if (last)
        {
            last->next = newPost;
            newPost->prev = last;
        }
        else
        {
            user->firstPost = newPost;
        }

        // update lastPost in map
        map.put(userName, user, newPost);

        // add to global singly-linked list (new head = most recent)
        newPost->globalNext = globalHead;
        globalHead = newPost;

        cout << "Post added: User " << userName << " -> [" << postId << "] : " << content << "\n";
    }

    void editPost(const string& userName, const string& password, int postId, const string& newContent)
    {
        if (!authenticate(userName, password))
        {
            cout << "Auth failed for user " << userName << "\n";
            return;
        }

        HashEntry* entry = map.get(userName);
        if (!entry)
        {
            cout << "User " << userName << " not found.\n";
            return;
        }

        Post* p = entry->userPtr->firstPost;
        while (p)
        {
            if (p->postId == postId)
            {
                p->content = newContent;
                cout << "Edited post " << postId << " of User " << userName << "\n";
                return;
            }
            p = p->next;
        }
        cout << "Post " << postId << " not found for user " << userName << "\n";
    }

    void deletePost(const string& userName, const string& password, int postId)
    {
        if (!authenticate(userName, password))
        {
            cout << "Auth failed for user " << userName << "\n";
            return;
        }

        HashEntry* entry = map.get(userName);
        if (!entry) return;

        Post* p = entry->userPtr->firstPost;
        while (p)
        {
            if (p->postId == postId)
            {
                // Unlink from user's doubly linked list
                if (p->prev) p->prev->next = p->next;
                if (p->next) p->next->prev = p->prev;
                if (p == entry->userPtr->firstPost) entry->userPtr->firstPost = p->next;

                // Update entry->lastPost if needed
                if (entry->lastPost == p)
                {
                    entry->lastPost = p->prev;
                    // write back to map (update stored lastPost)
                    map.put(userName, entry->userPtr, entry->lastPost);
                }

                // Unlink from global singly linked list
                if (p == globalHead)
                {
                    globalHead = p->globalNext;
                }
                else
                {
                    Post* g = globalHead;
                    while (g && g->globalNext != p) g = g->globalNext;
                    if (g) g->globalNext = p->globalNext;
                }

                cout << "Deleted post " << postId << " of User " << userName << "\n";
                delete p;
                return;
            }
            p = p->next;
        }
        cout << "Post " << postId << " not found for user " << userName << "\n";
    }

    void getMostRecentUserPost(const string& userName) const
    {
        HashEntry* entry = map.get(userName);
        if (!entry || !entry->lastPost)
        {
            cout << "No posts for user " << userName << "\n";
            return;
        }

        cout << "Most recent post of User " << userName
             << ": [" << entry->lastPost->postId << "] "
             << entry->lastPost->content << "\n";
    }

    void getMostRecentGlobalPost() const
    {
        if (!globalHead)
        {
            cout << "No global posts.\n";
            return;
        }

        cout << "Most recent global post: [" << globalHead->postId << "] "
             << globalHead->content << "\n";
    }

    void getRecentUserPost(const string& userName, int n) const
    {
        HashEntry* entry = map.get(userName);
        if (!entry || !entry->lastPost)
        {
            cout << "No posts for user " << userName << "\n";
            return;
        }

        cout << "Last " << n << " posts of User " << userName << ":\n";
        Post* p = entry->lastPost;
        int count = 0;
        while (p && count < n)
        {
            cout << "[" << p->postId << "] " << p->content << "\n";
            p = p->prev;
            ++count;
        }
        if (count < n) cout << "No more posts found.\n";
    }

    void getRecentGlobalPosts(int n) const
    {
        if (!globalHead)
        {
            cout << "No global posts.\n";
            return;
        }

        cout << "Last " << n << " global posts:\n";
        Post* p = globalHead;
        int count = 0;
        while (p && count < n)
        {
            cout << "[" << p->postId << "] " << p->content << "\n";
            p = p->globalNext;
            ++count;
        }
        if (count < n) cout << "No more posts found.\n";
    }
};

// helper input functions
int readInt(const string& prompt)
{
    string line;
    int value;
    while (true)
    {
        cout << prompt;
        if (!getline(cin, line))
        {
            cout << "\nInput closed. Exiting.\n";
            exit(0);
        }
        stringstream ss(line);
        if (ss >> value) return value;
        cout << "Invalid integer. Please try again.\n";
    }
}

string readLine(const string& prompt)
{
    string s;
    cout << prompt;
    if (!getline(cin, s))
    {
        cout << "\nInput closed. Exiting.\n";
        exit(0);
    }
    return s;
}

int main()
{
    SocialMedia sm;
    cout << "Interactive SocialMedia (menu-driven). Type clean inputs.\n";

    while (true)
    {
        cout << "\n===== MENU =====\n";
        cout << "1. Create User\n";
        cout << "2. Add Post\n";
        cout << "3. Edit Post\n";
        cout << "4. Delete Post\n";
        cout << "5. Show Most Recent Post of a User\n";
        cout << "6. Show Most Recent Global Post\n";
        cout << "7. Show 'N' Recent User Post\n";
        cout << "8. Show 'N' Recent Global Post\n";
        cout << "9. Exit\n";

        int choice = readInt("Enter choice: ");

        if (choice == 9)
        {
            cout << "Exiting...\n";
            break;
        }

        string userName, password, content;
        int postId, n;

        switch (choice)
        {
            case 1:
                userName = readLine("Enter username: ");
                password = readLine("Enter password: ");
                sm.createUser(userName, password);
                break;

            case 2:
                userName = readLine("Enter username: ");
                password = readLine("Enter password: ");
                postId = readInt("Enter post id: ");
                content = readLine("Enter content: ");
                sm.addPost(userName, password, postId, content);
                break;

            case 3:
                userName = readLine("Enter username: ");
                password = readLine("Enter password: ");
                postId = readInt("Enter post id to edit: ");
                content = readLine("Enter new content: ");
                sm.editPost(userName, password, postId, content);
                break;

            case 4:
                userName = readLine("Enter username: ");
                password = readLine("Enter password: ");
                postId = readInt("Enter post id to delete: ");
                sm.deletePost(userName, password, postId);
                break;

            case 5:
                userName = readLine("Enter username: ");
                sm.getMostRecentUserPost(userName);
                break;

            case 6:
                sm.getMostRecentGlobalPost();
                break;

            case 7:
                userName = readLine("Enter username: ");
                n = readInt("Enter N: ");
                sm.getRecentUserPost(userName, n);
                break;

            case 8:
                n = readInt("Enter N: ");
                sm.getRecentGlobalPosts(n);
                break;

            default:
                cout << "Invalid choice ...\n";
        }
    }
    return 0;
}
