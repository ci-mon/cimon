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
    setReadonly(value){
        this.quill.enable(!value);
    }
}
hljs.configure({
    languages: [
        'bash',
        'csharp',
        'diff',
        'dockerfile',
        'javascript',
        'markdown',
        'node-repl',
        'shell',
        'typescript',
        'xml',
        'yaml']
});
class ImageClick {
    constructor(quill, options) {
        this.quill = quill;
        this.handleClick = this.handleClick.bind(this);
        this.quill.root.addEventListener('click', this.handleClick, false);
    }

    handleClick(event) {
        if (event.target && event.target.tagName && event.target.tagName.toUpperCase() === 'IMG') {
            let bg = document.getElementById('ql-fullscreen-image-background');
            let ffsImg = document.getElementById('ql-fullscreen-image');
            ffsImg.style.backgroundImage = `url(${event.target.src})`;
            bg.classList.toggle('visible');
            bg.onclick = () => {
                bg.onclick = null;
                bg.classList.toggle('visible');
            };
        }
    }
}
Quill.register('modules/imageClick', ImageClick);
window.quillInterop = {
    initQuill: function (element, page, readonly) {
        const toolbar = readonly ? null :[
            [{header: [1, 2, false]}],
            ['bold', 'italic', 'underline'],
            ['link'],
            [{list: 'ordered'}, {list: 'bullet'}],
            ['clean'],
            ['image','code-block']
        ]; 
        const quill = new Quill(element, {
            modules: {
                imageClick: true,
                syntax: true,
                clipboard: {
                    matchers: [[Node.TEXT_NODE, urlMatcher]]
                },
                keyboard: {
                    bindings: {
                        send: {
                            key: 'enter',
                            ctrlKey: true,
                            handler: async function(range, context) {
                                await page.invokeMethodAsync('UpdateContent', this.quill.root.innerHTML);
                                await page.invokeMethodAsync('Send');
                            }
                        },
                    }
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
                        if (mentionChar === '#') {
                            const usersResponse = await fetch(`/api/users/searchTeams?q=${searchTerm}`);
                            const users = await usersResponse.json();
                            let values = users.map(x => ({id: x.name, value: x.name}));
                            renderList(values, searchTerm);
                        }
                    },
                },
                toolbar: toolbar,
            },
            placeholder: readonly ? undefined : 'Write something...',
            theme: 'snow',
        });
        if (readonly){
            quill.disable();
        }
        quill.on('text-change', function (delta, oldDelta, source) {
            if (source === 'user') {
                page.invokeMethodAsync('UpdateContent', quill.root.innerHTML);
            }
        });
        return new QuillInterop(quill);
    },
    
};
