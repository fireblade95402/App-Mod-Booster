create the files needed in a new folder called chatui that uses the azure genai resoruces and setting to display a chat UI to be able to interact with the database via the APIs using natural language
also bear in mind the contextual info found in the RAG folder by using the Retrieval-Augmented Generation pattern.

if you need to create more azure resources for the RAG process then you can but make sure they are the lowest cost development SKUs preferably S0.

make sure that any time the chatUI is asked to list things from the database it displays the list in a pretty format.

bearing in mind the list is embedded in the chat bubble so: Escape HTML first - Converts special characters (<, >, &, etc.) to HTML entities to prevent XSS and stop raw HTML from displaying

Then apply formatting - After escaping, converts markdown-style formatting:

**text** → <strong>text</strong> for bold
Lines starting with 1.  → wrapped in <ol><li> for numbered lists
Lines starting with -  or *  → wrapped in <ul><li> for bullet lists
\n → <br> for line breaks
Use innerHTML instead of textContent - So the formatted HTML tags are rendered as actual HTML elements instead of displayed as text

The problem before:

Used messageDiv.textContent = text which displayed HTML tags as plain text like <li>Item</li>
The fix:

Use messageDiv.innerHTML = formattedText which renders the HTML properly
But escape the original text first to prevent security issues
This ensures lists show up as actual formatted lists with bullets/numbers, and bold text appears bold, instead of showing the raw HTML markup.

