function isValidURL(string) {
    try {
        new URL(string);
        return true;
    } catch (_) {
        return false;
    }
}

const Delta = Quill.import('delta');

function urlMatcher(node, delta) {
    if (node.nodeType === Node.TEXT_NODE && isValidURL(node.data)) {
        return new Delta().insert(node.data, {link: node.data});
    } else {
        return delta;
    }
}

class QuillInterop {
    constructor(quill) {
        this.quill = quill;
    }
    getContent() {
        return this.quill.root.innerHTML;
    }
    setContent(content) {
        this.quill.root.innerHTML = content;
    }
}
window.quillInterop = {
    initQuill: function (element, contentChangedCallback) {
        const quill = new Quill(element, {
            modules: {
                clipboard: {
                    matchers: [[Node.TEXT_NODE, urlMatcher]]
                },
                mention: {
                    allowedChars: /^[A-Za-z\s]*$/,
                    mentionDenotationChars: ["@", "#"],
                    source: async function (searchTerm, renderList, mentionChar) {
                        if (mentionChar === '@') {
                            const usersResponse = await fetch(`/api/users/search?q=${searchTerm}`);
                            const users = await usersResponse.json();
                            let values = users.map(x => ({id: x.name, value: x.name}));
                            renderList(values, searchTerm);
                        }
                    },
                },
                toolbar: [
                    [{header: [1, 2, false]}],
                    ['bold', 'italic', 'underline'],
                    ['link'],
                    [{list: 'ordered'}, {list: 'bullet'}],
                    ['clean'],
                ],
            },
            theme: 'snow',
        });

        quill.on('text-change', function () {
            contentChangedCallback.invokeMethodAsync('UpdateContent', quill.root.innerHTML);
        });

        return new QuillInterop(quill);
    },
    
};
