import { createApp } from "https://unpkg.com/vue@3/dist/vue.esm-browser.js"

class ReconnectionPolicy {
    nextRetryDelayInMilliseconds(retryContext) {
        return 5000;
    }
}
createApp({
    data() {
        return {
            state: 'none',
            UserName: '',
            messages: [],
            mentions: []
        }
    },
    mounted() {
        this.connect();
    },
    methods: {
        getDiscussionUrl(item){
            return `/buildDiscussion/${item.buildId}`;
        },
        async connect() {
            const response = await fetch('/auth/token', {
                redirect: "manual"
            });
            var body = await response.json();
            var y = await fetch('auth/checkJwt', {
                headers: {
                    Authorization: `Bearer ${body.token}`
                }
            });
            /*await fetch('/auth/logoutUser', {
                method: 'POST',
                body: JSON.stringify({
                   userName: `TSCRM\\v.artemchuk`
               }),
              headers: {
                  Authorization: `Bearer ${body.token}`,
                  'Content-Type': 'application/json'
              }
            });*/

            this.UserName = body.userName;
            let connection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/user", {
                    accessTokenFactory: () => body.token
                })
                .withAutomaticReconnect(new ReconnectionPolicy())
                .build();
            connection.onclose(() => {
                this.state = 'onclose';
            });
            connection.onreconnected(async () => {
                this.state = 'onreconnected';
                await connection.invoke('SubscribeForMentions');
            });
            ////NotifyWithUrl(string url, string message)
            connection.on("NotifyWithUrl", (buildId, url, header, message)=>{
               this.messages.push({url, header, message})
            });
            connection.on("UpdateMentions", (mentions)=>{
               this.mentions = mentions;
            });
            connection.on('CheckForUpdates', (mentions)=>{
                this.messages.push({header: 'CheckForUpdates signal received'})
            });
            await connection.start();
            this.state = 'connected';

            //connection.invoke('ReplyToNotification', "buildId", 1, null);
            window.connection = connection;
            await connection.invoke('SubscribeForMentions');
            await connection.invoke('SubscribeForLastMonitor');
        }
    }
}).mount('#app')
