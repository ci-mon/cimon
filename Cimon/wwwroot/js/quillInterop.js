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
        var mentionUsers = [
            { id: 1, value: 'Alice' },
            { id: 2, value: 'Bob' },
            { id: 3, value: 'Charlie' },
            // Add more users as needed
        ];

        var quill = new Quill(element, {
            modules: {
                mention: {
                    allowedChars: /^[A-Za-z\s]*$/,
                    mentionDenotationChars: ["@"],
                    source: function (searchTerm, renderList, mentionChar) {
                        let values = mentionUsers;

                        if (searchTerm.length === 0) {
                            renderList(values, searchTerm);
                        } else {
                            const matches = [];
                            for (let i = 0; i < values.length; i++)
                                if (~values[i].value.toLowerCase().indexOf(searchTerm.toLowerCase())) matches.push(values[i]);
                            renderList(matches, searchTerm);
                        }
                    },
                },
                toolbar: [
                    [{ header: [1, 2, false] }],
                    ['bold', 'italic', 'underline'],
                    ['link'],
                    [{ list: 'ordered' }, { list: 'bullet' }],
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
